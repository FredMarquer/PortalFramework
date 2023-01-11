Shader "Hidden/Portal"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _CameraViewport("CameraViewport", Vector) = (0, 0, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On
        ZTest On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _CameraViewport;

            float4 frag(v2f i) : SV_TARGET
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                uv = _CameraViewport.xy + (_CameraViewport.zw * uv);
                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}
