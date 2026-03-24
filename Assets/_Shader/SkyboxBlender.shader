Shader "Skybox/CubemapBlender" {
    Properties {
        _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
        [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0
        _Blend ("Blend", Range(0, 1)) = 0
        [NoScaleOffset] _Tex ("Cubemap Day (1)", Cube) = "white" {}
        [NoScaleOffset] _Tex2 ("Cubemap Night (2)", Cube) = "white" {}
    }

    SubShader {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _Tex, _Tex2;
            half4 _Tint;
            half _Exposure, _Rotation, _Blend;

            struct appdata_t {
                float4 vertex : POSITION;
            };
            struct v2f {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            float3 RotateAroundYInDegrees (float3 vertex, float degrees) {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }

            v2f vert (appdata_t v) {
                v2f o;
                float3 rotated = RotateAroundYInDegrees(v.vertex.xyz, _Rotation);
                o.vertex = UnityObjectToClipPos(rotated);
                o.texcoord = v.vertex.xyz;
                return o;
            }

            half4 frag (v2f i) : SV_Target {
                half4 tex1 = texCUBE(_Tex, i.texcoord);
                half4 tex2 = texCUBE(_Tex2, i.texcoord);
                half4 res = lerp(tex1, tex2, _Blend);
                return res * _Tint * _Exposure;
            }
            ENDCG
        }
    }
}