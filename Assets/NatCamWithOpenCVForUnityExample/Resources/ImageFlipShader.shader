Shader "Hidden/NatCamWithOpenCVForUnity/ImageFlipShader"
{
    Properties {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _Mirror("Mirror", Vector) = (0, 0, 0, 0)
    }
    SubShader {
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            uniform sampler2D _MainTex;
            uniform fixed4 _Mirror;

            float4 frag(v2f_img i) : COLOR {
                // Flip
                i.uv.x = _Mirror.x > 0.5 ? (1 - i.uv.x) : i.uv.x;
                i.uv.y = _Mirror.y > 0.5 ? (1 - i.uv.y) : i.uv.y;
                return tex2D(_MainTex, i.uv);
            }

            ENDCG
        }
    }
}
