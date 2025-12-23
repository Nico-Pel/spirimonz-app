Shader "Custom/Billboard"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
        _Size ("Size", Float) = 1.0
        _RotationZ ("Rotation Z (deg)", Float) = 0.0
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
            fixed4 _Color;
            float _Size;
            float _RotationZ;

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

            v2f vert(appdata v)
            {
                v2f o;

                // position de l'objet dans le monde
                float3 worldPos = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;

                // vecteurs caméra
                float3 toCam = normalize(_WorldSpaceCameraPos - worldPos);
                float3 camRight = normalize(cross(float3(0,1,0), toCam));
                float3 camUp = cross(toCam, camRight);

                // rotation Z dans le plan du sprite
                float angle = _RotationZ * UNITY_PI / 180.0;
                float cosA = cos(angle);
                float sinA = sin(angle);
                float2 rotatedXY = float2(
                    v.vertex.x * cosA - v.vertex.y * sinA,
                    v.vertex.x * sinA + v.vertex.y * cosA
                );

                // offset final
                float3 offset = camRight * (rotatedXY.x * _Size) + camUp * (rotatedXY.y * _Size);

                float3 finalPos = worldPos + offset;

                o.vertex = UnityObjectToClipPos(float4(finalPos,1));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDCG
        }
    }
}