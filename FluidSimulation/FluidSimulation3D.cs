using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FluidSimulation3D : MonoBehaviour
{
    [Header("Simulation Parameters")]
    public int maxParticles = 8192;
    public float particleRadius = 0.1f;
    public float particleMass = 1.0f;
    public float restDensity = 1000f;
    public float viscosity = 0.1f;
    public float stiffness = 200f;
    public float damping = 0.9f;
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public Vector3 containerSize = new Vector3(10, 10, 10);
    
    [Header("Rendering")]
    public Material particleMaterial;
    public Mesh particleMesh;
    public float particleScale = 1f;
    
    [Header("Spawn Settings")]
    public Vector3 spawnArea = new Vector3(2, 2, 2);
    public Vector3 spawnOffset = Vector3.zero;
    public int particlesPerFrame = 50;
    
    private ComputeShader fluidComputeShader;
    private ComputeBuffer particleBuffer;
    private ComputeBuffer densityBuffer;
    private ComputeBuffer pressureBuffer;
    private ComputeBuffer forceBuffer;
    private ComputeBuffer neighborBuffer;
    private ComputeBuffer neighborCountBuffer;
    private ComputeBuffer gridBuffer;
    private ComputeBuffer gridCountBuffer;
    
    private int kernelDensity;
    private int kernelPressure;
    private int kernelForce;
    private int kernelIntegrate;
    private int kernelGrid;
    private int kernelNeighbors;
    
    private int activeParticles = 0;
    private Camera renderCamera;
    private RenderTexture depthTexture;
    private CommandBuffer commandBuffer;
    
    private struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
        public float life;
    }
    
    private struct FluidProperties
    {
        public float density;
        public float pressure;
        public Vector3 force;
        public float padding;
    }
    
    private const int THREAD_COUNT = 64;
    private const int MAX_NEIGHBORS = 64;
    private const int GRID_SIZE = 32;
    
    void Start()
    {
        InitializeCompute();
        InitializeBuffers();
        InitializeRendering();
        SpawnInitialParticles();
    }
    
    void InitializeCompute()
    {
        fluidComputeShader = Resources.Load<ComputeShader>("FluidSimulation");
        
        kernelGrid = fluidComputeShader.FindKernel("BuildGrid");
        kernelNeighbors = fluidComputeShader.FindKernel("FindNeighbors");
        kernelDensity = fluidComputeShader.FindKernel("ComputeDensity");
        kernelPressure = fluidComputeShader.FindKernel("ComputePressure");
        kernelForce = fluidComputeShader.FindKernel("ComputeForces");
        kernelIntegrate = fluidComputeShader.FindKernel("Integrate");
    }
    
    void InitializeBuffers()
    {
        particleBuffer = new ComputeBuffer(maxParticles, sizeof(float) * 10);
        densityBuffer = new ComputeBuffer(maxParticles, sizeof(float));
        pressureBuffer = new ComputeBuffer(maxParticles, sizeof(float));
        forceBuffer = new ComputeBuffer(maxParticles, sizeof(float) * 4);
        neighborBuffer = new ComputeBuffer(maxParticles * MAX_NEIGHBORS, sizeof(int));
        neighborCountBuffer = new ComputeBuffer(maxParticles, sizeof(int));
        gridBuffer = new ComputeBuffer(GRID_SIZE * GRID_SIZE * GRID_SIZE * 10, sizeof(int));
        gridCountBuffer = new ComputeBuffer(GRID_SIZE * GRID_SIZE * GRID_SIZE, sizeof(int));
        
        Particle[] particles = new Particle[maxParticles];
        for (int i = 0; i < maxParticles; i++)
        {
            particles[i] = new Particle
            {
                position = Vector3.zero,
                velocity = Vector3.zero,
                acceleration = Vector3.zero,
                life = 0f
            };
        }
        particleBuffer.SetData(particles);
        
        SetComputeShaderBuffers();
    }
    
    void SetComputeShaderBuffers()
    {
        int[] kernels = { kernelGrid, kernelNeighbors, kernelDensity, kernelPressure, kernelForce, kernelIntegrate };
        
        foreach (int kernel in kernels)
        {
            fluidComputeShader.SetBuffer(kernel, "particles", particleBuffer);
            fluidComputeShader.SetBuffer(kernel, "densities", densityBuffer);
            fluidComputeShader.SetBuffer(kernel, "pressures", pressureBuffer);
            fluidComputeShader.SetBuffer(kernel, "forces", forceBuffer);
            fluidComputeShader.SetBuffer(kernel, "neighbors", neighborBuffer);
            fluidComputeShader.SetBuffer(kernel, "neighborCounts", neighborCountBuffer);
            fluidComputeShader.SetBuffer(kernel, "grid", gridBuffer);
            fluidComputeShader.SetBuffer(kernel, "gridCounts", gridCountBuffer);
        }
    }
    
    void InitializeRendering()
    {
        renderCamera = Camera.main;
        if (renderCamera == null)
            renderCamera = FindObjectOfType<Camera>();
            
        commandBuffer = new CommandBuffer();
        commandBuffer.name = "Fluid Rendering";
        
        if (particleMaterial == null)
        {
            particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            particleMaterial.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            particleMaterial.SetFloat("_Surface", 1);
            particleMaterial.SetFloat("_Blend", 0);
        }
        
        if (particleMesh == null)
            particleMesh = CreateSphereMesh();
    }
    
    Mesh CreateSphereMesh()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh = sphere.GetComponent<MeshFilter>().mesh;
        DestroyImmediate(sphere);
        return mesh;
    }
    
    void SpawnInitialParticles()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int z = 0; z < 8; z++)
                {
                    if (activeParticles >= maxParticles) return;
                    
                    Vector3 pos = new Vector3(
                        x * particleRadius * 2.1f - spawnArea.x * 0.5f,
                        y * particleRadius * 2.1f + spawnOffset.y,
                        z * particleRadius * 2.1f - spawnArea.z * 0.5f
                    ) + spawnOffset;
                    
                    SpawnParticle(pos);
                }
            }
        }
    }
    
    void SpawnParticle(Vector3 position)
    {
        if (activeParticles >= maxParticles) return;
        
        Particle newParticle = new Particle
        {
            position = position,
            velocity = Vector3.zero,
            acceleration = Vector3.zero,
            life = 1f
        };
        
        Particle[] particles = new Particle[1];
        particles[0] = newParticle;
        particleBuffer.SetData(particles, 0, activeParticles, 1);
        
        activeParticles++;
    }
    
    void Update()
    {
        if (activeParticles == 0) return;
        
        UpdateComputeShaderParameters();
        RunSimulation();
        
        if (Input.GetMouseButton(0) && activeParticles < maxParticles)
        {
            Ray ray = renderCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 spawnPos = ray.origin + ray.direction * 5f;
            
            for (int i = 0; i < particlesPerFrame && activeParticles < maxParticles; i++)
            {
                Vector3 randomOffset = new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(-0.5f, 0.5f)
                ) * particleRadius;
                
                SpawnParticle(spawnPos + randomOffset);
            }
        }
    }
    
    void UpdateComputeShaderParameters()
    {
        float smoothingRadius = particleRadius * 4f;
        
        fluidComputeShader.SetInt("maxParticles", maxParticles);
        fluidComputeShader.SetInt("activeParticles", activeParticles);
        fluidComputeShader.SetFloat("particleRadius", particleRadius);
        fluidComputeShader.SetFloat("smoothingRadius", smoothingRadius);
        fluidComputeShader.SetFloat("particleMass", particleMass);
        fluidComputeShader.SetFloat("restDensity", restDensity);
        fluidComputeShader.SetFloat("viscosity", viscosity);
        fluidComputeShader.SetFloat("stiffness", stiffness);
        fluidComputeShader.SetFloat("damping", damping);
        fluidComputeShader.SetFloat("deltaTime", Time.fixedDeltaTime);
        fluidComputeShader.SetVector("gravity", gravity);
        fluidComputeShader.SetVector("containerSize", containerSize);
        fluidComputeShader.SetVector("containerCenter", transform.position);
        fluidComputeShader.SetInt("gridSize", GRID_SIZE);
        fluidComputeShader.SetFloat("gridCellSize", smoothingRadius);
    }
    
    void RunSimulation()
    {
        int threadGroups = Mathf.CeilToInt((float)activeParticles / THREAD_COUNT);
        int gridThreadGroups = Mathf.CeilToInt((float)(GRID_SIZE * GRID_SIZE * GRID_SIZE) / THREAD_COUNT);
        
        fluidComputeShader.SetInt("clearGrid", 1);
        fluidComputeShader.Dispatch(kernelGrid, gridThreadGroups, 1, 1);
        
        fluidComputeShader.SetInt("clearGrid", 0);
        fluidComputeShader.Dispatch(kernelGrid, threadGroups, 1, 1);
        
        fluidComputeShader.Dispatch(kernelNeighbors, threadGroups, 1, 1);
        fluidComputeShader.Dispatch(kernelDensity, threadGroups, 1, 1);
        fluidComputeShader.Dispatch(kernelPressure, threadGroups, 1, 1);
        fluidComputeShader.Dispatch(kernelForce, threadGroups, 1, 1);
        fluidComputeShader.Dispatch(kernelIntegrate, threadGroups, 1, 1);
    }
    
    void LateUpdate()
    {
        RenderParticles();
    }
    
    void RenderParticles()
    {
        if (activeParticles == 0 || particleMaterial == null || particleMesh == null) return;
        
        commandBuffer.Clear();
        
        Matrix4x4[] matrices = new Matrix4x4[activeParticles];
        Particle[] particles = new Particle[activeParticles];
        particleBuffer.GetData(particles, 0, 0, activeParticles);
        
        for (int i = 0; i < activeParticles; i++)
        {
            if (particles[i].life > 0)
            {
                Vector3 scale = Vector3.one * particleScale * particleRadius;
                matrices[i] = Matrix4x4.TRS(particles[i].position, Quaternion.identity, scale);
            }
            else
            {
                matrices[i] = Matrix4x4.zero;
            }
        }
        
        commandBuffer.DrawMeshInstanced(particleMesh, 0, particleMaterial, 0, matrices);
        Graphics.ExecuteCommandBuffer(commandBuffer);
    }
    
    void OnDestroy()
    {
        if (particleBuffer != null) particleBuffer.Release();
        if (densityBuffer != null) densityBuffer.Release();
        if (pressureBuffer != null) pressureBuffer.Release();
        if (forceBuffer != null) forceBuffer.Release();
        if (neighborBuffer != null) neighborBuffer.Release();
        if (neighborCountBuffer != null) neighborCountBuffer.Release();
        if (gridBuffer != null) gridBuffer.Release();
        if (gridCountBuffer != null) gridCountBuffer.Release();
        if (commandBuffer != null) commandBuffer.Release();
    }
    
    public void SetGravity(Vector3 newGravity)
    {
        gravity = newGravity;
    }
    
    public int GetActiveParticleCount()
    {
        return activeParticles;
    }
    
    public void SpawnParticlesAtMousePosition(int count)
    {
        if (renderCamera == null) return;
        
        Ray ray = renderCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 spawnPos = ray.origin + ray.direction * 5f;
        
        for (int i = 0; i < count && activeParticles < maxParticles; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f)
            ) * particleRadius * 2f;
            
            SpawnParticle(spawnPos + randomOffset);
        }
    }
    
    public void ClearAllParticles()
    {
        activeParticles = 0;
        
        Particle[] particles = new Particle[maxParticles];
        for (int i = 0; i < maxParticles; i++)
        {
            particles[i] = new Particle
            {
                position = Vector3.zero,
                velocity = Vector3.zero,
                acceleration = Vector3.zero,
                life = 0f
            };
        }
        particleBuffer.SetData(particles);
    }
    
    public void ResetSimulation()
    {
        ClearAllParticles();
        SpawnInitialParticles();
    }
    
    public void ReinitializeBuffers()
    {
        if (particleBuffer != null) particleBuffer.Release();
        if (densityBuffer != null) densityBuffer.Release();
        if (pressureBuffer != null) pressureBuffer.Release();
        if (forceBuffer != null) forceBuffer.Release();
        if (neighborBuffer != null) neighborBuffer.Release();
        if (neighborCountBuffer != null) neighborCountBuffer.Release();
        if (gridBuffer != null) gridBuffer.Release();
        if (gridCountBuffer != null) gridCountBuffer.Release();
        
        InitializeBuffers();
        SpawnInitialParticles();
    }
    
    void FixedUpdate()
    {
        if (activeParticles > 0)
        {
            UpdateComputeShaderParameters();
            RunSimulation();
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, containerSize);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + spawnOffset, spawnArea);
        
        if (activeParticles > 0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position + spawnOffset, particleRadius);
        }
    }
}