Shader "Custom/FluidParticle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.2, 0.6, 1, 0.8)
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.9
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 2
        _Refraction ("Refraction", Range(0, 1)) = 0.1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline"="UniversalRenderPipeline"
        }
        
        LOD 100
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Metallic;
                float _Smoothness;
                float _FresnelPower;
                float _Refraction;
            CBUFFER_END
            
            // GPU Instancing support - per-instance data
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _InstanceScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _InstanceMetallic)
                UNITY_DEFINE_INSTANCED_PROP(float, _InstanceSmoothness)
            UNITY_INSTANCING_BUFFER_END(Props)
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                // Apply per-instance scale
                float instanceScale = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceScale);
                float4 scaledPosition = input.positionOS;
                scaledPosition.xyz *= instanceScale;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(scaledPosition.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                output.shadowCoord = GetShadowCoord(positionInputs);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                float3 lightDirWS = normalize(mainLight.direction);
                float NdotL = saturate(dot(normalWS, lightDirWS));
                float NdotV = saturate(dot(normalWS, viewDirWS));
                
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                
                float3 reflectDir = reflect(-viewDirWS, normalWS);
                float3 halfDir = normalize(lightDirWS + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                
                // Get per-instance properties
                float4 instanceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
                float instanceMetallic = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceMetallic);
                float instanceSmoothness = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceSmoothness);
                
                // Combine base color with per-instance color
                float3 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb * _Color.rgb * instanceColor.rgb;
                
                float3 ambient = SampleSH(normalWS) * baseColor;
                
                float3 diffuse = baseColor * mainLight.color * NdotL * mainLight.shadowAttenuation;
                
                // Use per-instance smoothness
                float smoothness = lerp(_Smoothness, instanceSmoothness, instanceColor.a);
                float roughness = 1.0 - smoothness;
                float alpha = roughness * roughness;
                float alpha2 = alpha * alpha;
                float denom = NdotH * NdotH * (alpha2 - 1.0) + 1.0;
                float D = alpha2 / (PI * denom * denom);
                
                float k = (roughness + 1.0) * (roughness + 1.0) / 8.0;
                float G1L = NdotL / (NdotL * (1.0 - k) + k);
                float G1V = NdotV / (NdotV * (1.0 - k) + k);
                float G = G1L * G1V;
                
                // Use per-instance metallic
                float metallic = lerp(_Metallic, instanceMetallic, instanceColor.a);
                float3 F0 = lerp(0.04, baseColor, metallic);
                float3 F = F0 + (1.0 - F0) * pow(1.0 - saturate(dot(halfDir, viewDirWS)), 5.0);
                
                float3 specular = D * G * F / (4.0 * NdotL * NdotV + 0.001);
                specular *= mainLight.color * NdotL * mainLight.shadowAttenuation;
                
                uint additionalLightsCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0; lightIndex < additionalLightsCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    float3 additionalLightDir = normalize(light.direction);
                    float additionalNdotL = saturate(dot(normalWS, additionalLightDir));
                    
                    diffuse += baseColor * light.color * additionalNdotL * light.distanceAttenuation * light.shadowAttenuation;
                    
                    float3 additionalHalfDir = normalize(additionalLightDir + viewDirWS);
                    float additionalNdotH = saturate(dot(normalWS, additionalHalfDir));
                    
                    float additionalDenom = additionalNdotH * additionalNdotH * (alpha2 - 1.0) + 1.0;
                    float additionalD = alpha2 / (PI * additionalDenom * additionalDenom);
                    
                    float additionalG1L = additionalNdotL / (additionalNdotL * (1.0 - k) + k);
                    float additionalG = additionalG1L * G1V;
                    
                    float3 additionalF = F0 + (1.0 - F0) * pow(1.0 - saturate(dot(additionalHalfDir, viewDirWS)), 5.0);
                    
                    float3 additionalSpecular = additionalD * additionalG * additionalF / (4.0 * additionalNdotL * NdotV + 0.001);
                    specular += additionalSpecular * light.color * additionalNdotL * light.distanceAttenuation * light.shadowAttenuation;
                }
                
                float3 finalColor = ambient + diffuse + specular;
                
                finalColor = lerp(finalColor, finalColor * 1.5, fresnel);
                
                float finalAlpha = _Color.a * instanceColor.a * (1.0 - fresnel * 0.3);
                finalAlpha = saturate(finalAlpha);
                
                return float4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            
            ZWrite On
            ColorMask 0
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}