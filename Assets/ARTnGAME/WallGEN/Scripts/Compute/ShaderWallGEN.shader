// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

//
// Opaque surface shader for Wall
//
// Vertex format:
// position.xyz = vertex position
// texcoord0.xy = uv for texturing
// texcoord1.xy = uv for position/rotation/scale texture
//
// Position kernel outputs:
// .xyz = position
// .w   = random value (0-1)
//
// Rotation kernel outputs:
// .xyzw = rotation (quaternion)
//
// Scale kernel outputs:
// .xyz = scale factor
// .w   = random value (0-1)
//
Shader "Kvant/Wall/SurfaceWallGEN"
{
    Properties
    {
        _PositionTex  ("_PositionTex", 2D) = "black"{}
        _RotationTex  ("_RotationTex", 2D) = "red"{}
        _ScaleTex     ("_ScaleTex", 2D) = "white"{}

        [Enum(Single, 0, Random, 1)]
        _ColorMode    ("_ColorMode", Float) = 0
        _Color        ("_Color", Color) = (1, 1, 1, 1)
        _Color2       ("_Color2", Color) = (0.5, 0.5, 0.5, 1)

        _Metallic     ("_Metallic", Range(0,1)) = 0.5
        _Smoothness   ("_Smoothness", Range(0,1)) = 0.5

        _MainTex      ("_MainTex", 2D) = "white"{}
        _NormalMap    ("_NormalMap", 2D) = "bump"{}
        _NormalScale  ("_NormalScale", Range(0,2)) = 1
        _OcclusionMap ("_OcclusionMap", 2D) = "white"{}
        _OcclusionStr ("_OcclusionStr", Range(0,1)) = 1

			 _BumpMap("Normal Map", 2D) = "bump" {}

        [Toggle]
        _RandomUV     ("_RandomUV", Float) = 0

			skyAmbience("sky Ambience", Float) =1
			contrast("contrast", Float) = 1
			brightness("brightness", Float) = 1
			texAmbience("texture Ambience", Float) = 1
    }
    SubShader
    {
			Pass
		{
		Tags { "RenderType" = "Opaque" }

		CGPROGRAM

		//#pragma surface surf Standard vertex:vert nolightmap addshadow
		//#pragma shader_feature _ALBEDOMAP
		//#pragma shader_feature _NORMALMAP
		//#pragma shader_feature _OCCLUSIONMAP
					   	

			 #pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#pragma target 3.0

			// compile shader into multiple variants, with and without shadows
			// (we don't care about any lightmaps yet, so skip these variants)
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			// shadow helper functions and macros
			#include "AutoLight.cginc"


			sampler2D _PositionTex;
		sampler2D _RotationTex;
		sampler2D _ScaleTex;
		float2 _BufferOffset;

		half _ColorMode;
		half4 _Color;
		half4 _Color2;

		half _Metallic;
		half _Smoothness;

		sampler2D _MainTex;
		sampler2D _NormalMap;sampler2D _BumpMap;
		half _NormalScale;
		sampler2D _OcclusionMap;
		half _OcclusionStr;
		float skyAmbience;
		float contrast;
		float brightness;
		float texAmbience;


		half _RandomUV;

		// Quaternion multiplication.
		// http://mathworld.wolfram.com/Quaternion.html
		float4 qmul(float4 q1, float4 q2)
		{
			return float4(
				q2.xyz * q1.w + q1.xyz * q2.w + cross(q1.xyz, q2.xyz),
				q1.w * q2.w - dot(q1.xyz, q2.xyz)
			);
		}

		// Rotate a vector with a rotation quaternion.
		// http://mathworld.wolfram.com/Quaternion.html
		float3 rotate_vector(float3 v, float4 r)
		{
			float4 r_c = r * float4(-1, -1, -1, 1);
			return qmul(r, qmul(float4(v, 0), r_c)).xyz;
		}

		struct v2f {
			float3 worldPos : TEXCOORD0;
			// these three vectors will hold a 3x3 rotation matrix
			// that transforms from tangent to world space
			half3 tspace0 : TEXCOORD1; // tangent.x, bitangent.x, normal.x
			half3 tspace1 : TEXCOORD2; // tangent.y, bitangent.y, normal.y
			half3 tspace2 : TEXCOORD3; // tangent.z, bitangent.z, normal.z
			// texture coordinate for the normal map
			float2 uv : TEXCOORD4;
			float4 pos : SV_POSITION;
			half4 color : COLOR;
		};

		// vertex shader now also needs a per-vertex tangent vector.
		// in Unity tangents are 4D vectors, with the .w component used to
		// indicate direction of the bitangent vector.
		// we also need the texture coordinate.
		//v2f vert(float4 vertex : POSITION, float3 normal : NORMAL, float4 tangent : TANGENT, float2 uv : TEXCOORD0)
		v2f vert(appdata_full v)//v2f vert(inout appdata_full v)
		{
			v2f o;

			//ROT
			float4 uvA = float4(v.texcoord1 + _BufferOffset, 0, 0);
			float4 p = tex2Dlod(_PositionTex, uvA);
			float4 r = tex2Dlod(_RotationTex, uvA);
			float4 s = tex2Dlod(_ScaleTex, uvA);
			v.vertex.xyz = rotate_vector(v.vertex.xyz * s.xyz, r) + p.xyz;
			v.normal = rotate_vector(v.normal, r);
#if _NORMALMAP
			v.tangent.xyz = rotate_vector(v.tangent.xyz, r);
#endif
			//v.color = lerp(_Color, _Color2, p.w * _ColorMode);
			//uv.xy += float2(p.w, s.w) * _RandomUV;
			o.color = lerp(_Color, _Color2, p.w * _ColorMode);
			v.texcoord.xy += float2(p.w, s.w) * _RandomUV;


			o.pos = UnityObjectToClipPos(v.vertex);
			o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			half3 wNormal = UnityObjectToWorldNormal(v.normal);
			half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
			// compute bitangent from cross product of normal and tangent
			half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
			half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
			// output the tangent space matrix
			o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
			o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
			o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
			//o.uv = uv;
			o.uv = v.texcoord;

			//	//ROT
			//	float4 uvA = float4(uv.xy + _BufferOffset, 0, 0);
			//	float4 p = tex2Dlod(_PositionTex, uvA);
			//	float4 r = tex2Dlod(_RotationTex, uvA);
			//	float4 s = tex2Dlod(_ScaleTex, uvA);
			//	vertex.xyz = rotate_vector(vertex.xyz * s.xyz, r) + p.xyz;
			//	normal = rotate_vector(normal, r);
			//	#if _NORMALMAP
			//					tangent.xyz = rotate_vector(tangent.xyz, r);
			//	#endif
			//	//v.color = lerp(_Color, _Color2, p.w * _ColorMode);
			//	uv.xy += float2(p.w, s.w) * _RandomUV;


			//o.pos = UnityObjectToClipPos(vertex);
			//o.worldPos = mul(unity_ObjectToWorld, vertex).xyz;
			//half3 wNormal = UnityObjectToWorldNormal(normal);
			//half3 wTangent = UnityObjectToWorldDir(tangent.xyz);
			//// compute bitangent from cross product of normal and tangent
			//half tangentSign = tangent.w * unity_WorldTransformParams.w;
			//half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
			//// output the tangent space matrix
			//o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
			//o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
			//o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
			//o.uv = uv;
			return o;
		}

		// normal map texture from shader properties
		//sampler2D _BumpMap;

		fixed4 frag(v2f i) : SV_Target
		{
			// sample the normal map, and decode from the Unity encoding
			half3 tnormal = UnpackNormal(tex2D(_BumpMap, i.uv)) * _NormalScale;
			// transform normal from tangent to world space
			half3 worldNormal;
			worldNormal.x = dot(i.tspace0, tnormal);
			worldNormal.y = dot(i.tspace1, tnormal);
			worldNormal.z = dot(i.tspace2, tnormal);

			// rest the same as in previous shader
			half3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
			half3 worldRefl = reflect(-worldViewDir, worldNormal);
			half4 skyData = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, worldRefl) * skyAmbience;
			half3 skyColor = DecodeHDR(skyData, unity_SpecCube0_HDR);
			fixed4 c = 0;
			c.rgb = skyColor;

			//c = c+0.1;// float4(1, 0, 0, 1);
			float4 col = tex2D(_MainTex, i.uv)* texAmbience;

			return pow(c + col, contrast)*brightness;
			//return pow(col, contrast)*brightness + c;
			//return c;
		}

		//TRY 1
		
		//	struct v2f
		//	{
		//		float2 uv : TEXCOORD0;
		//		SHADOW_COORDS(1) // put shadows data into TEXCOORD1
		//		fixed3 diff : COLOR0;
		//		fixed3 ambient : COLOR1;
		//		float4 pos : SV_POSITION;
		//	};
		//	v2f vert(appdata_base v)
		//	{
		//		v2f o;



		//		//ROT
		//		float4 uv = float4(v.texcoord.xy + _BufferOffset, 0, 0);
		//		float4 p = tex2Dlod(_PositionTex, uv);
		//		float4 r = tex2Dlod(_RotationTex, uv);
		//		float4 s = tex2Dlod(_ScaleTex, uv);
		//		v.vertex.xyz = rotate_vector(v.vertex.xyz * s.xyz, r) + p.xyz;
		//		v.normal = rotate_vector(v.normal, r);
		//		//#if _NORMALMAP
		//		//				v.tangent.xyz = rotate_vector(v.tangent.xyz, r);
		//		//#endif
		//		//v.color = lerp(_Color, _Color2, p.w * _ColorMode);
		//		v.texcoord.xy += float2(p.w, s.w) * _RandomUV;



		//		o.pos = UnityObjectToClipPos(v.vertex);
		//		o.uv = v.texcoord;
		//		half3 worldNormal = UnityObjectToWorldNormal(v.normal);
		//		half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
		//		o.diff = nl * _LightColor0.rgb;
		//		o.ambient = ShadeSH9(half4(worldNormal,1));


		//		



		//		// compute shadows data
		//		TRANSFER_SHADOW(o)
		//		return o;
		//	}

		//	sampler2D _MainTex;

		//	fixed4 frag(v2f i) : SV_Target
		//	{
		//		fixed4 col = tex2D(_MainTex, i.uv);
		//	// compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
		//	fixed shadow = SHADOW_ATTENUATION(i);
		//	// darken light's illumination with shadow, keep ambient intact
		//	fixed3 lighting = i.diff * shadow + i.ambient;
		//	col.rgb *= lighting*5;
		//	return col;
		//}

		//END TRY 1



		/*struct Input
		{
			float2 uv_MainTex;
			half4 color : COLOR;
		};

		void vert(inout appdata_full v)
		{
			float4 uv = float4(v.texcoord1.xy + _BufferOffset, 0, 0);

			float4 p = tex2Dlod(_PositionTex, uv);
			float4 r = tex2Dlod(_RotationTex, uv);
			float4 s = tex2Dlod(_ScaleTex, uv);

			v.vertex.xyz = rotate_vector(v.vertex.xyz * s.xyz, r) + p.xyz;
			v.normal = rotate_vector(v.normal, r);
		#if _NORMALMAP
			v.tangent.xyz = rotate_vector(v.tangent.xyz, r);
		#endif
			v.color = lerp(_Color, _Color2, p.w * _ColorMode);
			v.texcoord.xy += float2(p.w, s.w) * _RandomUV;
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
		#if _ALBEDOMAP
			half4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = IN.color.rgb * c.rgb;
			o.Alpha = IN.color.a * c.a;
		#else
			o.Albedo = IN.color.rgb;
			o.Alpha = IN.color.a;
		#endif

		#if _NORMALMAP
			half4 n = tex2D(_NormalMap, IN.uv_MainTex);
			o.Normal = UnpackScaleNormal(n, _NormalScale);
		#endif

		#if _OCCLUSIONMAP
			half4 occ = tex2D(_OcclusionMap, IN.uv_MainTex);
			o.Occlusion = lerp((half4)1, occ, _OcclusionStr);
		#endif

			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
		}*/

		ENDCG
			}//ENDPASS




			// shadow casting support
			Pass
			{
				Tags{ "LightMode" = "ShadowCaster" }
				CGPROGRAM
				 #pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#pragma target 3.0

				// compile shader into multiple variants, with and without shadows
				// (we don't care about any lightmaps yet, so skip these variants)

				//SHADOW
				//#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
				#pragma multi_compile_shadowcaster

				// shadow helper functions and macros
				#include "AutoLight.cginc"


				sampler2D _PositionTex;
			sampler2D _RotationTex;
			sampler2D _ScaleTex;
			float2 _BufferOffset;

			half _ColorMode;
			half4 _Color;
			half4 _Color2;

			half _Metallic;
			half _Smoothness;

			sampler2D _MainTex;
			sampler2D _NormalMap; sampler2D _BumpMap;
			half _NormalScale;
			sampler2D _OcclusionMap;
			half _OcclusionStr;

			half _RandomUV;

			// Quaternion multiplication.
			// http://mathworld.wolfram.com/Quaternion.html
			float4 qmul(float4 q1, float4 q2)
			{
				return float4(
					q2.xyz * q1.w + q1.xyz * q2.w + cross(q1.xyz, q2.xyz),
					q1.w * q2.w - dot(q1.xyz, q2.xyz)
				);
			}

			// Rotate a vector with a rotation quaternion.
			// http://mathworld.wolfram.com/Quaternion.html
			float3 rotate_vector(float3 v, float4 r)
			{
				float4 r_c = r * float4(-1, -1, -1, 1);
				return qmul(r, qmul(float4(v, 0), r_c)).xyz;
			}

			struct v2f {
				float3 worldPos : TEXCOORD0;
				// these three vectors will hold a 3x3 rotation matrix
				// that transforms from tangent to world space
				half3 tspace0 : TEXCOORD1; // tangent.x, bitangent.x, normal.x
				half3 tspace1 : TEXCOORD2; // tangent.y, bitangent.y, normal.y
				half3 tspace2 : TEXCOORD3; // tangent.z, bitangent.z, normal.z
				// texture coordinate for the normal map
				float2 uv : TEXCOORD4;
				//float4 pos : SV_POSITION;
				half4 color : COLOR;
				V2F_SHADOW_CASTER;
			};

			// vertex shader now also needs a per-vertex tangent vector.
			// in Unity tangents are 4D vectors, with the .w component used to
			// indicate direction of the bitangent vector.
			// we also need the texture coordinate.
			//v2f vert(float4 vertex : POSITION, float3 normal : NORMAL, float4 tangent : TANGENT, float2 uv : TEXCOORD0)
			v2f vert(appdata_full v)//v2f vert(inout appdata_full v)
			{
				v2f o;

				//ROT
				float4 uvA = float4(v.texcoord1 + _BufferOffset, 0, 0);
				float4 p = tex2Dlod(_PositionTex, uvA);
				float4 r = tex2Dlod(_RotationTex, uvA);
				float4 s = tex2Dlod(_ScaleTex, uvA);
				v.vertex.xyz = rotate_vector(v.vertex.xyz * s.xyz, r) + p.xyz;
				v.normal = rotate_vector(v.normal, r);
	#if _NORMALMAP
				v.tangent.xyz = rotate_vector(v.tangent.xyz, r);
	#endif
				//v.color = lerp(_Color, _Color2, p.w * _ColorMode);
				//uv.xy += float2(p.w, s.w) * _RandomUV;
				o.color = lerp(_Color, _Color2, p.w * _ColorMode);
				v.texcoord.xy += float2(p.w, s.w) * _RandomUV;


				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				half3 wNormal = UnityObjectToWorldNormal(v.normal);
				half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
				// compute bitangent from cross product of normal and tangent
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
				// output the tangent space matrix
				o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
				o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
				o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
				//o.uv = uv;
				o.uv = v.texcoord;

				//	//ROT
				//	float4 uvA = float4(uv.xy + _BufferOffset, 0, 0);
				//	float4 p = tex2Dlod(_PositionTex, uvA);
				//	float4 r = tex2Dlod(_RotationTex, uvA);
				//	float4 s = tex2Dlod(_ScaleTex, uvA);
				//	vertex.xyz = rotate_vector(vertex.xyz * s.xyz, r) + p.xyz;
				//	normal = rotate_vector(normal, r);
				//	#if _NORMALMAP
				//					tangent.xyz = rotate_vector(tangent.xyz, r);
				//	#endif
				//	//v.color = lerp(_Color, _Color2, p.w * _ColorMode);
				//	uv.xy += float2(p.w, s.w) * _RandomUV;


				//o.pos = UnityObjectToClipPos(vertex);
				//o.worldPos = mul(unity_ObjectToWorld, vertex).xyz;
				//half3 wNormal = UnityObjectToWorldNormal(normal);
				//half3 wTangent = UnityObjectToWorldDir(tangent.xyz);
				//// compute bitangent from cross product of normal and tangent
				//half tangentSign = tangent.w * unity_WorldTransformParams.w;
				//half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
				//// output the tangent space matrix
				//o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
				//o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
				//o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
				//o.uv = uv;

				//SHADOW
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)

				return o;
			}

			// normal map texture from shader properties
			//sampler2D _BumpMap;

			fixed4 frag(v2f i) : SV_Target
			{

				SHADOW_CASTER_FRAGMENT(i)

				// sample the normal map, and decode from the Unity encoding
				//half3 tnormal = UnpackNormal(tex2D(_BumpMap, i.uv));
				//// transform normal from tangent to world space
				//half3 worldNormal;
				//worldNormal.x = dot(i.tspace0, tnormal);
				//worldNormal.y = dot(i.tspace1, tnormal);
				//worldNormal.z = dot(i.tspace2, tnormal);

				//// rest the same as in previous shader
				//half3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
				//half3 worldRefl = reflect(-worldViewDir, worldNormal);
				//half4 skyData = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, worldRefl);
				//half3 skyColor = DecodeHDR(skyData, unity_SpecCube0_HDR);
				//fixed4 c = 0;
				//c.rgb = skyColor;

				////float4 col = tex2D(_MainTex, i.uv);

				//return c;
			}

				ENDCG
			}
			//UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
   // CustomEditor "Kvant.WallMaterialEditor"
}
