Shader "ARTnGAME/Ivy/DoubleSidedShaderIVY_GROW"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
			_BumpMap("Normalmap", 2D) = "bump" {}
		_Shininess("Shininess", Float) = 0.078125//_Shininess("Shininess", Range(0.01, 1)) = 0.078125

		//_Color("Color", Color) = (1, 1, 1, 1)	
		_growStage("_growStage", Range(-1.0, 0.2)) = 0
		_Thickness("_Thickness", float) = 0
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 200
				cull off
			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma surface surf Standard vertex:vert fullforwardshadows
			//#pragma surface surf BlinnPhong fullforwardshadows //alphatest:_Cutoff

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
		sampler2D _BumpMap;

		//v0.8
		float4 _Color;		
		float _growStage;
		float _Thickness;


        struct Input
        {
            float2 uv_MainTex;
			float2 uv_BumpMap;
        };

        half _Glossiness;
        half _Metallic;
        //fixed4 _Color;
		half _Shininess;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

   //     void surf (Input IN, inout SurfaceOutputStandard o) //SurfaceOutput o ) //SurfaceOutputStandard o)
   //     {
   //         // Albedo comes from a texture tinted by color
   //         fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color * 1.4;
   //         o.Albedo = saturate(c.rgb);
   //         // Metallic and smoothness come from slider variables
   //        
   //        
   //         o.Alpha = saturate(c.a);
			//o.Normal = saturate(UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap)) * 1);

			////o.Gloss = 0;//o.Gloss = tex.a;
			////o.Alpha = tex.a * _Color.a;
			////o.Specular = 0;// _Shininess;

			//o.Smoothness = saturate(_Glossiness);
			//o.Metallic = saturate(_Metallic);
   //     }

		//GROW

		void vert(inout appdata_full v) {
			v.vertex.xyz = v.vertex.xyz - _Thickness*v.normal * smoothstep(0.0, 0.5, v.texcoord.y - _growStage);
		}

		void surf(Input IN, inout SurfaceOutputStandard o) {
			half4 c = tex2D(_MainTex, IN.uv_MainTex + float2(0, -_growStage));			
			o.Normal = saturate(UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap)));
			o.Albedo = c.rgb * _Color* 1.4;
			o.Alpha = 1 - saturate(IN.uv_MainTex.x - _growStage);	
			o.Smoothness = saturate(_Glossiness);
			o.Metallic = saturate(_Metallic);
			clip(o.Alpha - 0.05);

		}

		//END GROW


        ENDCG
    }
    FallBack "Diffuse"
}
