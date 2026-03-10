Shader "Custom/StandardDetailColor"
{
    Properties
    {
        _MainTex ("Shirt Texture (UV1)", 2D) = "white" {}
        _Color ("Shirt Color", Color) = (1,1,1,1)

        _DetailTex ("Logo Texture (UV2)", 2D) = "white" {}
        _DetailColor ("Logo Color", Color) = (1,1,1,1)

        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        sampler2D _MainTex;
        sampler2D _DetailTex;

        fixed4 _Color;
        fixed4 _DetailColor;

        half _Metallic;
        half _Smoothness;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_DetailTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Base shirt texture
            fixed4 baseTex = tex2D(_MainTex, IN.uv_MainTex);
            fixed3 shirt = baseTex.rgb * _Color.rgb;

            // Logo texture using UV2
            fixed4 logoTex = tex2D(_DetailTex, IN.uv2_DetailTex);

            // Use logo texture as mask
            fixed3 logo = logoTex.rgb * _DetailColor.rgb;
            float mask = logoTex.a;

            // Combine independently
            fixed3 finalColor = lerp(shirt, logo, mask);

            o.Albedo = finalColor;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = 1;
        }
        ENDCG
    }

    FallBack "Standard"
}