Shader "Custom/EchoSegmentScanSurface"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.02, 0.02, 0.025, 1)
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
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            fixed4 _EchoColor;

            float _EchoRadius;
            float _ConeAngleRad;
            float _ConeSoftness;
            float _RingWidth;
            float _WaveTrail;
            float _WaveFade;
            float _EchoIntensity;

            float4 _HeadPos;
            float4 _HeadForward;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 origin = _HeadPos.xyz;
                float3 offset = i.worldPos - origin;
                float dist = length(offset);

                if (dist < 0.01)
                {
                    return fixed4(_BaseColor.rgb, 1);
                }

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

                fixed3 color = _BaseColor.rgb + _EchoColor.rgb * scan;
                return fixed4(color, 1);
            }

            ENDCG
        }
    }
}
