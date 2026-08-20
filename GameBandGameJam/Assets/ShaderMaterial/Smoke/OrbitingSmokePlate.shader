Shader "Custom/URP/OrbitingSmokePlate"
{
    Properties
    {
        [HDR] _SmokeColor ("Smoke Color", Color) = (0.65, 0.75, 1.0, 0.8)

        _Speed       ("Orbit Speed", Range(-5, 5)) = 0.6
        _NoiseScale  ("Noise Scale", Range(0.5, 12)) = 4.0
        _Distortion  ("Distortion", Range(0, 4)) = 1.4
        _Spiral      ("Spiral Amount", Range(-10, 10)) = 3.0

        _Cutoff      ("Smoke Cutoff", Range(0, 1)) = 0.32
        _Contrast    ("Smoke Contrast", Range(0.1, 8)) = 2.8
        _Opacity     ("Opacity", Range(0, 1)) = 0.8

        _InnerRadius ("Inner Radius", Range(0, 0.9)) = 0.0
        _EdgeSoftness("Edge Softness", Range(0.001, 0.5)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"

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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _SmokeColor;

                float _Speed;
                float _NoiseScale;
                float _Distortion;
                float _Spiral;

                float _Cutoff;
                float _Contrast;
                float _Opacity;

                float _InnerRadius;
                float _EdgeSoftness;
            CBUFFER_END

            float2 Rotate2D(float2 p, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);

                return float2(
                    c * p.x - s * p.y,
                    s * p.x + c * p.y
                );
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);

                local = local * local * (3.0 - 2.0 * local);

                float bottomLeft  = Hash21(cell);
                float bottomRight = Hash21(cell + float2(1.0, 0.0));
                float topLeft     = Hash21(cell + float2(0.0, 1.0));
                float topRight    = Hash21(cell + float2(1.0, 1.0));

                float bottom = lerp(bottomLeft, bottomRight, local.x);
                float top    = lerp(topLeft, topRight, local.x);

                return lerp(bottom, top, local.y);
            }

            float FBM(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;

                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    value += ValueNoise(p) * amplitude;

                    p = Rotate2D(p, 0.67);
                    p *= 2.03;

                    amplitude *= 0.5;
                }

                return value;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv = input.uv;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Convert UV coordinates from 0..1 to -1..1.
                float2 centeredUV = (input.uv - 0.5) * 2.0;

                float radius = length(centeredUV);
                float time = _Time.y * _Speed;

                /*
                 * Rotate all noise around the center.
                 * Adding radius to the rotation creates the spiral shape.
                 */
                float orbitAngle = time + radius * _Spiral;

                float2 noiseUV = Rotate2D(centeredUV, -orbitAngle);
                noiseUV *= _NoiseScale;

                // Two noise samples create domain warping.
                float2 warp;

                warp.x = FBM(
                    Rotate2D(noiseUV, time * 0.13) +
                    float2(2.7, 6.4)
                );

                warp.y = FBM(
                    Rotate2D(noiseUV, -time * 0.09) +
                    float2(8.3, 1.5)
                );

                noiseUV += (warp - 0.5) * _Distortion;

                float smoke = FBM(noiseUV);

                // Extra high-frequency detail.
                float detail = FBM(
                    Rotate2D(noiseUV * 1.8, -time * 0.2) +
                    4.7
                );

                smoke = lerp(smoke, detail, 0.25);

                // Convert noise into visible smoke patches.
                float smokeDensity =
                    saturate((smoke - _Cutoff) * _Contrast);

                float feather = max(_EdgeSoftness, 0.0001);

                // Circular outer edge.
                float outerMask =
                    1.0 - smoothstep(1.0 - feather, 1.0, radius);

                // Optional central opening.
                float innerMask = smoothstep(
                    _InnerRadius,
                    _InnerRadius + feather,
                    radius
                );

                innerMask = lerp(
                    1.0,
                    innerMask,
                    step(0.001, _InnerRadius)
                );

                float plateMask = outerMask * innerMask;

                float alpha =
                    smokeDensity *
                    plateMask *
                    _Opacity *
                    _SmokeColor.a;

                // Slight brightness variation based on smoke density.
                half brightness = lerp(0.4h, 1.2h, smokeDensity);
                half3 color = _SmokeColor.rgb * brightness;

                return half4(color, alpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}