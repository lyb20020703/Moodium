// Standard shader with triplanar mapping
// https://github.com/keijiro/StandardTriplanar

Shader "Standard Triplanar"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _MainTex("Main Texture", 2D) = "white" {}

		 _DetailTexA("Detail Texture", 2D) = "white" {}
		 _ColorA("Color A", Color) = (1, 1, 1, 1)

		 _DetailTexB("Detail Texture B", 2D) = "white" {}
		 _ColorB("Color B", Color) = (1, 1, 1, 1)

        _Glossiness("_Glossiness", Range(0, 1)) = 0.5
        [Gamma] _Metallic("_Metallic", Range(0, 1)) = 0

        _BumpScale("_BumpScale", Float) = 1
        _BumpMap("_BumpMap", 2D) = "bump" {}

        _OcclusionStrength("_OcclusionStrength", Range(0, 1)) = 1
        _OcclusionMap("_OcclusionMap", 2D) = "white" {}

        _MapScale("_MapScale", Float) = 1

			//v0.1
			_MapScaleA("_MapScale A", Float) = 1
			_MapScaleB("_MapScale B", Float) = 1
			colorAPower("Color A Power", Float) = 1
			colorBPower("Color B Power", Float) = 1
			vertexPushA("vertex Push A", Float) = 1
			vertexPushHeightA("vertex Push Height A", Float) = 1
			vertexPushB("vertex Push B", Float) = 1
			vertexPushHeightB("vertex Push Height B", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        CGPROGRAM

        #pragma surface surf Standard vertex:vert fullforwardshadows addshadow

        #pragma shader_feature _NORMALMAP
        #pragma shader_feature _OCCLUSIONMAP

        #pragma target 3.0

        half4 _Color;
        sampler2D _MainTex;  
		sampler2D _DetailTexA;

		float vertexPushHeightA;
		float vertexPushA;
		float4 _ColorA;
		float _MapScaleA;
		float colorAPower;

		sampler2D _DetailTexB;
		float4 _ColorB;
		float vertexPushHeightB;
		float vertexPushB;
		float _MapScaleB;
		float colorBPower;

        half _Glossiness;
        half _Metallic;

        half _BumpScale;
        sampler2D _BumpMap;

        half _OcclusionStrength;
        sampler2D _OcclusionMap;

        half _MapScale;

        struct Input
        {
            float3 localCoord;
            float3 localNormal;
        };

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            data.localCoord = v.vertex.xyz;
            data.localNormal = v.normal.xyz;



			//DETAIL A - v0.1
			float3 bf = normalize(abs(data.localNormal));
			bf /= dot(bf, (float3)1);

			// Triplanar mapping
			float2 tx = data.localCoord.yz * _MapScaleA;
			float2 ty = data.localCoord.zx * _MapScaleA;
			float2 tz = data.localCoord.xy * _MapScaleA;
			half4 cxA = tex2Dlod(_DetailTexA, tx.xyyy) * bf.x;
			half4 cyA = tex2Dlod(_DetailTexA, ty.xyyy) * bf.y;
			half4 czA = tex2Dlod(_DetailTexA, tz.xyyy) * bf.z;
			half4 colorA = (cxA + cyA + czA) * _ColorA;
			if (v.vertex.y	 < vertexPushHeightA) {
				v.vertex.xyz = v.vertex.xyz + vertexPushA * v.normal.xyz*colorA.rgb;
			}


			//DETAIL B - v0.1
			float2 txB = data.localCoord.yz * _MapScaleB;
			float2 tyB = data.localCoord.zx * _MapScaleB;
			float2 tzB = data.localCoord.xy * _MapScaleB;
			half4 cxB = tex2Dlod(_DetailTexB, txB.xyyy) * bf.x;
			half4 cyB = tex2Dlod(_DetailTexB, tyB.xyyy) * bf.y;
			half4 czB = tex2Dlod(_DetailTexB, tzB.xyyy) * bf.z;
			half4 colorB = (cxB + cyB + czB) * _ColorB;
			if (v.vertex.y < vertexPushHeightB) {
				v.vertex.xyz = v.vertex.xyz + vertexPushB * v.normal.xyz*colorB.rgb;
			}
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Blending factor of triplanar mapping
            float3 bf = normalize(abs(IN.localNormal));
            bf /= dot(bf, (float3)1);

            // Triplanar mapping
            float2 tx = IN.localCoord.yz * _MapScale;
            float2 ty = IN.localCoord.zx * _MapScale;
            float2 tz = IN.localCoord.xy * _MapScale;

            // Base color
            half4 cx = tex2D(_MainTex, tx) * bf.x;
            half4 cy = tex2D(_MainTex, ty) * bf.y;
            half4 cz = tex2D(_MainTex, tz) * bf.z;
			half4 color = (cx + cy + cz) * _Color;


			//DETAIL A - v0.1
			float2 txA = IN.localCoord.yz * _MapScaleA;
			float2 tyA = IN.localCoord.zx * _MapScaleA;
			float2 tzA = IN.localCoord.xy * _MapScaleA;
            half4 cxA = tex2D(_DetailTexA, txA) * bf.x;
            half4 cyA = tex2D(_DetailTexA, tyA) * bf.y;
            half4 czA = tex2D(_DetailTexA, tzA) * bf.z;
			half4 colorA = (cxA + cyA + czA) * _ColorA;

			//DETAIL A - v0.1
			float2 txB = IN.localCoord.yz * _MapScaleB;
			float2 tyB = IN.localCoord.zx * _MapScaleB;
			float2 tzB = IN.localCoord.xy * _MapScaleB;
			half4 cxB = tex2D(_DetailTexB, txB) * bf.x;
			half4 cyB = tex2D(_DetailTexB, tyB) * bf.y;
			half4 czB = tex2D(_DetailTexB, tzB) * bf.z;
			half4 colorB = (cxB + cyB + czB) * _ColorB;

            
            o.Albedo = color.rgb + colorA * colorAPower + colorB * colorBPower;

            o.Alpha = color.a;

        #ifdef _NORMALMAP
            // Normal map
            half4 nx = tex2D(_BumpMap, tx) * bf.x;
            half4 ny = tex2D(_BumpMap, ty) * bf.y;
            half4 nz = tex2D(_BumpMap, tz) * bf.z;
            o.Normal = UnpackScaleNormal(nx + ny + nz, _BumpScale);
        #endif

        #ifdef _OCCLUSIONMAP
            // Occlusion map
            half ox = tex2D(_OcclusionMap, tx).g * bf.x;
            half oy = tex2D(_OcclusionMap, ty).g * bf.y;
            half oz = tex2D(_OcclusionMap, tz).g * bf.z;
            o.Occlusion = lerp((half4)1, ox + oy + oz, _OcclusionStrength);
        #endif

            // Misc parameters
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
   // CustomEditor "StandardTriplanarInspector"
}
