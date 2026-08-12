Shader "Custom/Toon Flowing Water"
{
    Properties
    {
        [Header(Water Colors)]
        _ShallowColor ("Shallow Color", Color) = (0.15, 0.75, 0.9, 0.75)
        _DeepColor ("Deep Color", Color) = (0.02, 0.25, 0.55, 0.85)
        _FresnelColor ("Edge Color", Color) = (0.2, 0.85, 1.0, 1.0)
        _WaterAlpha ("Water Alpha", Range(0, 1)) = 0.8

        [Header(Flow)]
        _FlowDirection ("Flow Direction XY", Vector) = (1, 0.25, 0, 0)
        _FlowSpeed ("Flow Speed", Range(0, 5)) = 0.5
        _NoiseScale ("Noise Scale", Range(0.1, 20)) = 2.5

        [Header(Surface Foam)]
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamThreshold ("Foam Threshold", Range(0, 1)) = 0.68
        _FoamSoftness ("Foam Softness", Range(0.001, 0.25)) = 0.035
        _FoamStrength ("Foam Strength", Range(0, 1)) = 1.0
        _FoamSpeed ("Foam Speed", Range(0, 5)) = 1.0

        [Header(Geometry Waves)]
        _WaveHeight ("Wave Height", Range(0, 1)) = 0.08
        _WaveFrequency ("Wave Frequency", Range(0.1, 10)) = 1.5
        _WaveSpeed ("Wave Speed", Range(0, 10)) = 1.2

        [Header(Edge Fresnel)]
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3.0
        _FresnelStrength ("Fresnel Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float fogFactor : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FresnelColor;
                float _WaterAlpha;

                float4 _FlowDirection;
                float _FlowSpeed;
                float _NoiseScale;

                float4 _FoamColor;
                float _FoamThreshold;
                float _FoamSoftness;
                float _FoamStrength;
                float _FoamSpeed;

                float _WaveHeight;
                float _WaveFrequency;
                float _WaveSpeed;

                float _FresnelPower;
                float _FresnelStrength;
            CBUFFER_END

            // Produces a repeatable pseudo-random value.
            float Hash21(float2 position)
            {
                position = frac(position * float2(123.34, 456.21));
                position += dot(position, position + 45.32);

                return frac(position.x * position.y);
            }

            // Smooth procedural value noise.
            float ValueNoise(float2 position)
            {
                float2 cell = floor(position);
                float2 localPosition = frac(position);

                float valueA = Hash21(cell);
                float valueB = Hash21(cell + float2(1.0, 0.0));
                float valueC = Hash21(cell + float2(0.0, 1.0));
                float valueD = Hash21(cell + float2(1.0, 1.0));

                float2 smoothPosition =
                    localPosition * localPosition *
                    (3.0 - 2.0 * localPosition);

                float bottom = lerp(valueA, valueB, smoothPosition.x);
                float top = lerp(valueC, valueD, smoothPosition.x);

                return lerp(bottom, top, smoothPosition.y);
            }

            // Combines several noise layers.
            float FlowingNoise(float2 position)
            {
                float noiseValue = 0.0;

                noiseValue += ValueNoise(position) * 0.55;
                noiseValue += ValueNoise(position * 2.03 + 17.13) * 0.30;
                noiseValue += ValueNoise(position * 4.01 - 9.17) * 0.15;

                return noiseValue;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS =
                    TransformObjectToWorld(input.positionOS.xyz);

                float time = _Time.y * _WaveSpeed;

                float waveA = sin(
                    dot(positionWS.xz, float2(1.0, 0.35))
                    * _WaveFrequency + time
                );

                float waveB = sin(
                    dot(positionWS.xz, float2(-0.4, 1.0))
                    * (_WaveFrequency * 1.37) - time * 0.8
                );

                float waveC = sin(
                    dot(positionWS.xz, float2(0.7, -0.65))
                    * (_WaveFrequency * 0.63) + time * 1.2
                );

                float combinedWave =
                    waveA * 0.5 +
                    waveB * 0.3 +
                    waveC * 0.2;

                positionWS.y += combinedWave * _WaveHeight;

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.fogFactor =
                    ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float time = _Time.y;

                // Use world-space XZ coordinates so the effect does not
                // depend on the water mesh's UV coordinates.
                float2 worldCoordinates =
                    input.positionWS.xz * _NoiseScale;

                float2 flowDirection =
                    normalize(_FlowDirection.xy + float2(0.0001, 0.0001));

                float2 primaryFlow =
                    flowDirection * time * _FlowSpeed;

                float2 secondaryDirection =
                    float2(-flowDirection.y, flowDirection.x);

                float2 secondaryFlow =
                    secondaryDirection * time * _FlowSpeed * 0.37;

                float primaryNoise =
                    FlowingNoise(worldCoordinates + primaryFlow);

                float secondaryNoise =
                    FlowingNoise(
                        worldCoordinates * 1.43 -
                        secondaryFlow +
                        float2(14.3, -8.7)
                    );

                float combinedNoise =
                    primaryNoise * 0.65 +
                    secondaryNoise * 0.35;

                // Slightly vary the water color using the noise.
                float colorVariation =
                    smoothstep(0.2, 0.85, combinedNoise);

                float3 waterColor = lerp(
                    _DeepColor.rgb,
                    _ShallowColor.rgb,
                    colorVariation
                );

                // Create moving white surface patterns.
                float foamNoise = FlowingNoise(
                    worldCoordinates * 1.2 +
                    primaryFlow * _FoamSpeed
                );

                // Break up the white shapes with a second flow direction.
                float foamBreakup = FlowingNoise(
                    worldCoordinates * 2.1 -
                    secondaryFlow * _FoamSpeed +
                    float2(4.6, 11.2)
                );

                foamNoise =
                    foamNoise * 0.75 +
                    foamBreakup * 0.25;

                float softness = max(_FoamSoftness, 0.0001);

                float foamMask = smoothstep(
                    _FoamThreshold - softness,
                    _FoamThreshold + softness,
                    foamNoise
                );

                foamMask *= _FoamStrength;

                // Fresnel-like coloring around viewing angles.
                float3 viewDirection = normalize(
                    _WorldSpaceCameraPos.xyz - input.positionWS
                );

                // This shader assumes the water generally faces upward.
                float fresnel = 1.0 - saturate(abs(viewDirection.y));
                fresnel = pow(fresnel, _FresnelPower);
                fresnel *= _FresnelStrength;

                waterColor = lerp(
                    waterColor,
                    _FresnelColor.rgb,
                    fresnel
                );

                // Add the white surface foam.
                float3 finalColor = lerp(
                    waterColor,
                    _FoamColor.rgb,
                    saturate(foamMask)
                );

                finalColor = MixFog(
                    finalColor,
                    input.fogFactor
                );

                float finalAlpha = saturate(
                    _WaterAlpha +
                    fresnel * 0.15 +
                    foamMask * 0.2
                );

                return half4(finalColor, finalAlpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}