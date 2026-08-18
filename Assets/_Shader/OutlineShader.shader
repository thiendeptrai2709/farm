Shader "Custom/Outline" 
{
    Properties 
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 0, 1)
        _OutlineThickness ("Outline Thickness", Range(0.0, 0.5)) = 0.05
    }
    
    SubShader 
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        
        Pass 
        {
            Name "Outline"
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes 
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings 
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineThickness;
            CBUFFER_END

            Varyings vert(Attributes input) 
            {
                Varyings output;
                float3 pos = input.positionOS.xyz + normalize(input.positionOS.xyz + float3(0, 0.0001, 0)) * _OutlineThickness;
                output.positionCS = TransformObjectToHClip(pos);
                return output;
            }

            half4 frag(Varyings input) : SV_Target 
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}