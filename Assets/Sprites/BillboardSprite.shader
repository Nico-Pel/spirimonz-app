Shader "Custom/BillboardWithColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1) // <-- nouvelle option couleur
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Off
            ZWrite Off
            Lighting Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color; // couleur depuis le material

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float3 camRight = UNITY_MATRIX_V._m00_m01_m02;
                float3 camUp    = UNITY_MATRIX_V._m10_m11_m12;

                float size = 1.0;

                float3 billboardPos = worldPos
                    + camRight * (v.vertex.x * size)
                    + camUp    * (v.vertex.y * size);

                o.vertex = UnityObjectToClipPos(float4(billboardPos, 1));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);
                return texColor * _Color; // applique le tint
            }
            ENDCG
        }
    }
}