Shader "Custom/EchoSegmentScanSurface"
{
    Properties
    {
        [MainTexture] _MainTex ("Albedo", 2D) = "white" {}
        _AlbedoStrength ("Albedo Strength", Range(0, 1)) = 0.35
        _BaseColor ("Base Tint", Color) = (0.02, 0.02, 0.025, 1)
        _EchoColor ("Echo Color", Color) = (0, 1, 1, 1)
        _EchoRadius ("Echo Radius", Float) = -100
        _ConeAngleRad ("Cone Angle Rad", Float) = 0.35
        _ConeSoftness ("Cone Softness", Float) = 0.08
        _RingWidth ("Ring Width", Float) = 0.45
        _WaveTrail ("Wave Trail", Float) = 1.1
        _WaveFade ("Wave Fade", Float) = 0.65
        _EchoIntensity ("Echo Intensity", Float) = 4.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
            "UniversalMaterialType" = "Unlit"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _AlbedoStrength;
                half4 _BaseColor;
                half4 _EchoColor;
                float _EchoRadius;
                float _ConeAngleRad;
                float _ConeSoftness;
                float _RingWidth;
                float _WaveTrail;
                float _WaveFade;
                float _EchoIntensity;
                float4 _HeadPos;
                float4 _HeadForward;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 origin = _HeadPos.xyz;
                float3 offset = input.positionWS - origin;
                float dist = length(offset);

                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb * _AlbedoStrength;
                half3 surface = max(albedo * _BaseColor.rgb, 0.06);

                if (dist >= 0.01)
                {
                    float3 dir = offset / dist;
                    float3 forward = normalize(_HeadForward.xyz);
                    float cosAngle = dot(dir, forward);
                    float cosLimit = cos(_ConeAngleRad);

                    float coneMask = smoothstep(
                        cosLimit - _ConeSoftness,
                        cosLimit + _ConeSoftness * 0.25,
                        cosAngle
                    );

                    float front = _EchoRadius;
                    float behindFront = front - dist;

                    float ring = 1.0 - smoothstep(0.0, _RingWidth, abs(behindFront));
                    ring = pow(saturate(ring), 1.4);

                    float trail = smoothstep(0.0, _WaveTrail, behindFront);
                    trail *= 1.0 - smoothstep(_WaveTrail, _WaveTrail + _WaveFade, behindFront);
                    trail = pow(saturate(trail), 1.2);

                    float scan = (ring * 1.15 + trail * 0.7) * coneMask;
                    scan = saturate(scan) * _EchoIntensity;

                    surface += _EchoColor.rgb * scan;
                }

                half3 color = MixFog(surface, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
