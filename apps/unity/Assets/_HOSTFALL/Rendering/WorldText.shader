Shader "HOSTFALL/World Text"
{
    Properties
    {
        [PerRendererData] _MainTex ("Font atlas", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth test", Float) = 4
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "WorldText"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest [_ZTest]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(input.positionOS.xyz); o.uv=input.uv; o.color=input.color*_Color; return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).a;
                return half4(input.color.rgb,input.color.a*alpha);
            }
            ENDHLSL
        }
    }
}
