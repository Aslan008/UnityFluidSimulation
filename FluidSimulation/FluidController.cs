using UnityEngine;
using UnityEngine.UI;

public class FluidController : MonoBehaviour
{
    [Header("UI References")]
    public Slider particleCountSlider;
    public Slider viscositySlider;
    public Slider stiffnessSlider;
    public Slider dampingSlider;
    public Slider gravitySlider;
    public Text particleCountText;
    public Text viscosityText;
    public Text stiffnessText;
    public Text dampingText;
    public Text gravityText;
    public Button resetButton;
    public Button pauseButton;
    public Toggle windToggle;
    
    [Header("Simulation Reference")]
    public FluidSimulation3D fluidSimulation;
    
    [Header("Wind Settings")]
    public Vector3 windForce = new Vector3(2f, 0f, 0f);
    public float windStrength = 1f;
    public bool enableWind = false;
    
    [Header("Performance")]
    public Text fpsText;
    public Text particleActiveText;
    
    private bool isPaused = false;
    private float originalTimeScale;
    private int frameCount = 0;
    private float timeAccumulator = 0f;
    private float currentFPS = 0f;
    
    void Start()
    {
        originalTimeScale = Time.timeScale;
        SetupUI();
        UpdateUI();
    }
    
    void SetupUI()
    {
        if (particleCountSlider != null)
        {
            particleCountSlider.minValue = 512;
            particleCountSlider.maxValue = 16384;
            particleCountSlider.value = fluidSimulation.maxParticles;
            particleCountSlider.onValueChanged.AddListener(OnParticleCountChanged);
        }
        
        if (viscositySlider != null)
        {
            viscositySlider.minValue = 0.01f;
            viscositySlider.maxValue = 1f;
            viscositySlider.value = fluidSimulation.viscosity;
            viscositySlider.onValueChanged.AddListener(OnViscosityChanged);
        }
        
        if (stiffnessSlider != null)
        {
            stiffnessSlider.minValue = 50f;
            stiffnessSlider.maxValue = 1000f;
            stiffnessSlider.value = fluidSimulation.stiffness;
            stiffnessSlider.onValueChanged.AddListener(OnStiffnessChanged);
        }
        
        if (dampingSlider != null)
        {
            dampingSlider.minValue = 0.1f;
            dampingSlider.maxValue = 1f;
            dampingSlider.value = fluidSimulation.damping;
            dampingSlider.onValueChanged.AddListener(OnDampingChanged);
        }
        
        if (gravitySlider != null)
        {
            gravitySlider.minValue = -20f;
            gravitySlider.maxValue = 0f;
            gravitySlider.value = fluidSimulation.gravity.y;
            gravitySlider.onValueChanged.AddListener(OnGravityChanged);
        }
        
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetSimulation);
            
        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);
            
        if (windToggle != null)
        {
            windToggle.isOn = enableWind;
            windToggle.onValueChanged.AddListener(OnWindToggled);
        }
    }
    
    void Update()
    {
        UpdatePerformanceStats();
        HandleInput();
        
        if (enableWind)
        {
            ApplyWindForce();
        }
    }
    
    void UpdatePerformanceStats()
    {
        frameCount++;
        timeAccumulator += Time.unscaledDeltaTime;
        
        if (timeAccumulator >= 1f)
        {
            currentFPS = frameCount / timeAccumulator;
            frameCount = 0;
            timeAccumulator = 0f;
        }
        
        if (fpsText != null)
            fpsText.text = $"FPS: {currentFPS:F1}";
            
        if (particleActiveText != null && fluidSimulation != null)
            particleActiveText.text = $"Active Particles: {fluidSimulation.GetActiveParticleCount()}";
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            TogglePause();
            
        if (Input.GetKeyDown(KeyCode.R))
            ResetSimulation();
            
        if (Input.GetKeyDown(KeyCode.W))
        {
            enableWind = !enableWind;
            if (windToggle != null)
                windToggle.isOn = enableWind;
        }
        
        if (Input.GetKey(KeyCode.LeftArrow))
            windForce.x = -windStrength;
        else if (Input.GetKey(KeyCode.RightArrow))
            windForce.x = windStrength;
        else
            windForce.x = 0f;
            
        if (Input.GetKey(KeyCode.UpArrow))
            windForce.z = windStrength;
        else if (Input.GetKey(KeyCode.DownArrow))
            windForce.z = -windStrength;
        else
            windForce.z = 0f;
    }
    
    void ApplyWindForce()
    {
        if (fluidSimulation != null)
        {
            Vector3 totalGravity = fluidSimulation.gravity + windForce;
            fluidSimulation.SetGravity(totalGravity);
        }
    }
    
    void OnParticleCountChanged(float value)
    {
        if (fluidSimulation != null)
        {
            fluidSimulation.maxParticles = (int)value;
            fluidSimulation.ReinitializeBuffers();
        }
        UpdateUI();
    }
    
    void OnViscosityChanged(float value)
    {
        if (fluidSimulation != null)
            fluidSimulation.viscosity = value;
        UpdateUI();
    }
    
    void OnStiffnessChanged(float value)
    {
        if (fluidSimulation != null)
            fluidSimulation.stiffness = value;
        UpdateUI();
    }
    
    void OnDampingChanged(float value)
    {
        if (fluidSimulation != null)
            fluidSimulation.damping = value;
        UpdateUI();
    }
    
    void OnGravityChanged(float value)
    {
        if (fluidSimulation != null)
        {
            Vector3 newGravity = fluidSimulation.gravity;
            newGravity.y = value;
            fluidSimulation.gravity = newGravity;
        }
        UpdateUI();
    }
    
    void OnWindToggled(bool enabled)
    {
        enableWind = enabled;
        if (!enabled && fluidSimulation != null)
        {
            fluidSimulation.SetGravity(new Vector3(0, fluidSimulation.gravity.y, 0));
        }
    }
    
    void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : originalTimeScale;
        
        if (pauseButton != null)
        {
            Text buttonText = pauseButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = isPaused ? "Resume" : "Pause";
        }
    }
    
    void ResetSimulation()
    {
        if (fluidSimulation != null)
        {
            fluidSimulation.ResetSimulation();
        }
        
        isPaused = false;
        Time.timeScale = originalTimeScale;
        
        if (pauseButton != null)
        {
            Text buttonText = pauseButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = "Pause";
        }
    }
    
    void UpdateUI()
    {
        if (particleCountText != null && fluidSimulation != null)
            particleCountText.text = $"Max Particles: {fluidSimulation.maxParticles}";
            
        if (viscosityText != null && fluidSimulation != null)
            viscosityText.text = $"Viscosity: {fluidSimulation.viscosity:F2}";
            
        if (stiffnessText != null && fluidSimulation != null)
            stiffnessText.text = $"Stiffness: {fluidSimulation.stiffness:F0}";
            
        if (dampingText != null && fluidSimulation != null)
            dampingText.text = $"Damping: {fluidSimulation.damping:F2}";
            
        if (gravityText != null && fluidSimulation != null)
            gravityText.text = $"Gravity: {fluidSimulation.gravity.y:F1}";
    }
    
    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 100, 30), "Add Particles"))
        {
            if (fluidSimulation != null)
                fluidSimulation.SpawnParticlesAtMousePosition(50);
        }
        
        if (GUI.Button(new Rect(10, 50, 100, 30), "Clear All"))
        {
            if (fluidSimulation != null)
                fluidSimulation.ClearAllParticles();
        }
        
        GUI.Label(new Rect(10, 90, 200, 20), $"Controls:");
        GUI.Label(new Rect(10, 110, 200, 20), $"Space - Pause/Resume");
        GUI.Label(new Rect(10, 130, 200, 20), $"R - Reset");
        GUI.Label(new Rect(10, 150, 200, 20), $"W - Toggle Wind");
        GUI.Label(new Rect(10, 170, 200, 20), $"Arrow Keys - Wind Direction");
        GUI.Label(new Rect(10, 190, 200, 20), $"Mouse Click - Spawn Particles");
    }
}