Shader "HOSTFALL/Energy Field"
{
    Properties { _BaseColor("Field colour", Color) = (0.08,0.65,1,1) _Heat("Heat", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor; float _Heat;
            CBUFFER_END
            Varyings vert(Attributes input)
            { Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.uv=input.uv;return o; }
            half4 frag(Varyings input):SV_Target
            {
                float band=step(.945,frac(input.uv.y*31-_Time.y*1.3));
                float edge=pow(abs(input.uv.x-.5)*2,10);
                float pulse=.86+.14*sin(_Time.y*5+input.uv.y*9);
                half3 colour=lerp(_BaseColor.rgb,half3(1,.22,.06),_Heat*.7);
                return half4(colour*pulse,(.12+band*.54+edge*.24)*_BaseColor.a);
            }
            ENDHLSL
        }
    }
}
