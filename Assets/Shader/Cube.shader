Shader "Custom/Cube"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.5, 0.5, 0.5, 1)
        _TextureInfluence ("Texture Influence", Range(0, 1)) = 0.7
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.5
        _FresnelPower ("Fresnel Power", Range(0, 10)) = 5
        _FresnelColor ("Fresnel Color", Color) = (1, 1, 1, 1)
        _BandingLevels ("Banding Levels", Range(1, 10)) = 4
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 2
        _PatternScale ("Pattern Scale", Range(0.1, 10)) = 1
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.1
        _ScanlineSpeed ("Scanline Speed", Range(0, 10)) = 1
        _MetallicMap ("Metallic Map (R)", 2D) = "white" {}
        _MetallicStrength ("Metallic Strength", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _TextureInfluence;
                float _ReflectionStrength;
                float _FresnelPower;
                float4 _FresnelColor;
                float _BandingLevels;
                float _NoiseScale;
                float _PatternScale;
                float _ScanlineStrength;
                float _ScanlineSpeed;
                float _MetallicStrength;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_MetallicMap);
            SAMPLER(sampler_MetallicMap);

            // Simple noise function for retro patterns
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Banding function to create limited color palette
            float band(float value, float levels)
            {
                return floor(value * levels) / levels;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;
                
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                output.uv = input.uv;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample the main texture
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float metallicMap = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, input.uv).r;
                
                // Normalize vectors
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                
                // Basic lighting
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = max(0, dot(normal, lightDir));
                
                // Fresnel effect for edges
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);
                
                // Create a simple reflection vector
                float3 reflectDir = reflect(-viewDir, normal);
                float reflection = saturate(dot(reflectDir, lightDir));
                
                // Procedural pattern for retro look
                float2 patternUV = input.uv * _PatternScale;
                float pattern = noise(patternUV);
                
                // Scanlines for that retro CRT feel
                float scanline = sin((input.positionHCS.y + _Time.y * _ScanlineSpeed) * 10) * 0.5 + 0.5;
                scanline = 1.0 - scanline * _ScanlineStrength;
                
                // Combine texture with base color
                float3 baseAlbedo = lerp(_BaseColor.rgb, texColor.rgb * _BaseColor.rgb, _TextureInfluence);
                
                // Combine all elements
                float3 metalColor = baseAlbedo;
                metalColor += pattern * 0.1; // Add some texture variation
                metalColor *= NdotL * mainLight.color * mainLight.shadowAttenuation;
                
                // Use metallic map to control reflections
                float reflectionStrength = _ReflectionStrength * metallicMap * _MetallicStrength;
                metalColor += reflection * reflectionStrength;
                metalColor += fresnel * _FresnelColor.rgb * metallicMap;
                metalColor *= scanline;
                
                // Apply color banding for limited palette
                metalColor.r = band(metalColor.r, _BandingLevels);
                metalColor.g = band(metalColor.g, _BandingLevels);
                metalColor.b = band(metalColor.b, _BandingLevels);
                
                return half4(metalColor, 1.0);
            }
            ENDHLSL
        }

        // Shadow pass
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}