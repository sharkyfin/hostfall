Shader "HOSTFALL/Archive Monitor"
{
    Properties { _ArchiveTex ("Recording", 2D) = "black" {} _FrameRect ("Frame UV", Vector) = (0, 0, .25, .33333) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            TEXTURE2D(_ArchiveTex);
            SAMPLER(sampler_ArchiveTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _FrameRect;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float2 inset = clamp(input.uv, .003, .997);
                half3 scene = SAMPLE_TEXTURE2D(_ArchiveTex, sampler_ArchiveTex, inset * _FrameRect.zw + _FrameRect.xy).rgb;
                half scan = .92h + .08h * sin(input.uv.y * 540);
                return half4(scene * scan * half3(.90h, 1, .94h), 1);
            }
            ENDHLSL
        }
    }
}
