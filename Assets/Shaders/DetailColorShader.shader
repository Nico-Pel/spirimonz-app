Shader "Custom/StandardDetailColor"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}

        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5

        _BumpMap ("Normal Map", 2D) = "bump" {}

        _DetailTex ("Detail Albedo", 2D) = "gray" {}
        _DetailColor ("Detail Color", Color) = (1,1,1,1)
        _DetailStrength ("Detail Strength", Range(0,2)) = 1

        _UseUV2 ("Use UV2 for Detail", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        sampler2D _MainTex;
        sampler2D _DetailTex;
        sampler2D _BumpMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_DetailTex;
            float2 uv2_DetailTex;
        };

        half _Glossiness;
        half _Metallic;

        fixed4 _Color;

        fixed4 _DetailColor;
        float _DetailStrength;
        float _UseUV2;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 detailUV = (_UseUV2 > 0.5) ? IN.uv2_DetailTex : IN.uv_DetailTex;

            fixed4 albedo = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            fixed4 detail = tex2D(_DetailTex, detailUV) * _DetailColor;
            detail *= _DetailStrength;

            fixed3 finalColor = albedo.rgb + detail.rgb;

            o.Albedo = finalColor;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Alpha = albedo.a;
        }
        ENDCG
    }

    FallBack "Standard"
}