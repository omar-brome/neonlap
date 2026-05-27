Shader "Hidden/NeonLap/NeonTrackEdgeBloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NeonPulse ("Pulse", Range(0, 3)) = 1
        _Intensity ("Intensity", Range(0, 2)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "NeonTrackEdgeBloom"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float _NeonPulse;
            float _Intensity;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 color = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, input.uv).rgb;
                half lum = dot(color, half3(0.2126, 0.7152, 0.0722));
                half neonMask = saturate((color.g + color.b * 1.2 - color.r * 0.35) - 0.35);
                neonMask *= saturate(lum - 0.25);
                half pulse = _NeonPulse * _Intensity;
                half3 glow = half3(0.2, 0.95, 1.0) * neonMask * pulse;
                return half4(color + glow, 1.0);
            }
            ENDHLSL
        }
    }
}
