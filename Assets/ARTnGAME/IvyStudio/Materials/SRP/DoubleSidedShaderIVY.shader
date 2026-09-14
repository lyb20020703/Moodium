Shader "ARTnGAME/Ivy/DoubleSidedShaderIVY"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
			_BumpMap("Normalmap", 2D) = "bump" {}
		_Shininess("Shininess", Float) = 0.078125//_Shininess("Shininess", Range(0.01, 1)) = 0.078125
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 200
				cull off
			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma surface surf Standard fullforwardshadows
			//#pragma surface surf BlinnPhong fullforwardshadows //alphatest:_Cutoff

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
		sampler2D _BumpMap;

        struct Input
        {
            float2 uv_MainTex;
			float2 uv_BumpMap;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
		half _Shininess;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o) //SurfaceOutput o ) //SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color * 1.4;
            o.Albedo = saturate(c.rgb);
            // Metallic and smoothness come from slider variables
           
           
            o.Alpha = saturate(c.a);
			o.Normal = saturate(UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap)) * 1);

			//o.Gloss = 0;//o.Gloss = tex.a;
			//o.Alpha = tex.a * _Color.a;
			//o.Specular = 0;// _Shininess;

			o.Smoothness = saturate(_Glossiness);
			o.Metallic = saturate(_Metallic);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
