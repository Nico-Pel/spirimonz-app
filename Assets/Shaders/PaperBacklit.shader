Shader "Custom/PaperBacklit"
{
    Properties
    {
        _Color("Tint", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        _BacklightColor("Backlight Color", Color) = (1,0.95,0.85,1)
        _BacklightStrength("Backlight Strength", Range(0, 2)) = 0.7
        _BacklightPower("Backlight Power", Range(0.5, 8)) = 2
        _AmbientBoost("Ambient Boost", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        LOD 200
        Cull Off

        CGPROGRAM
        #pragma surface surf Paper fullforwardshadows addshadow
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _BacklightColor;
        half _BacklightStrength;
        half _BacklightPower;
        half _AmbientBoost;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = 1;
        }

        inline half4 LightingPaper(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half ndotl = dot(s.Normal, lightDir);
            half front = saturate(ndotl);
            half back = pow(saturate(-ndotl), _BacklightPower) * _BacklightStrength;
            half3 light = _LightColor0.rgb * (front + back) * atten;
            half3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * _AmbientBoost;

            half4 c;
            c.rgb = s.Albedo * (light + ambient) + s.Emission;
            c.a = s.Alpha;
            return c;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
