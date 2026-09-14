// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_LightmapInd', a built-in variable
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D
// Upgrade NOTE: replaced tex2D unity_LightmapInd with UNITY_SAMPLE_TEX2D_SAMPLER

Shader "InfiniGRASS/InfiniGrass Directional Wind ROOF SHAPING MASK NOISE IVY URP" {
    Properties {
        _Diffuse ("Diffuse", 2D) = "white" {}
        _Normal ("Normal", 2D) = "bump" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        
        _BulgeScale ("Bulge Scale", Float ) = 0.2
        _BulgeShape ("Bulge Shape", Float ) = 5
        _BulgeScale_copy ("Bulge Scale_copy", Float ) = 1.2 // control turbulence
                               
        _WaveControl1("Waves", Vector) = (1, 0.01, 0.001, 0.41) // _WaveControl1.w controls interaction power
        _TimeControl1("Time", Vector) = (1, 1, 1, 100)
        _OceanCenter("Ocean Center", Vector) = (0, 0, 0, 0)
        
         _RandYScale ("Vary Height Ammount", Float ) = 1
         _RippleScale ("Vary Height", Float ) = 0     
        
           //INFINIGRASS - hero position for fading and lowering dynamics to reduce jitter while interacting  
           
           _InteractPos("Interact Position", Vector) = (0, 0, 0) //for lowering motion when interaction item is near
            _InteractSpeed("Interact Speed", Vector) = (0, 0, 0) //v1.5
           _FadeThreshold ("Fade out Threshold", Float ) = 100
           _StopMotionThreshold ("Stop motion Threshold", Float ) = 10
           
           [HDR]_Color ("Grass tint", Color) = (0.5,0.8,0.5,0) //0.5,0.8,0.5
           [HDR]_ColorGlobal ("Global tint", Color) = (0.5,0.5,0.5,0) //0.5,0.8,0.5
           _TintPower("tint power", Float) = 0
            _TintFrequency("tint frequency", Float) = 0.1
           _SpecularPower("Specular", Float) = 1
           
           _SmoothMotionFactor("Smooth wave motion", Float) = 105
           _WaveXFactor("Wave Control x axis", Float) = 1
           _WaveYFactor("Wave Control y axis", Float) = 1
           
           //SNOW
           _SnowTexture ("Snow texture", 2D) = "white" {}
		   _SnowOffset("Snow Offset", Float) = 0

           //ROOF version of FENCE shader - control local vs global wind
           //_TimeControl1.y for global, _TimeControl1.z for local
           _BaseLight("Grass Base light control", Float) = 0
           _BaseColorYShift("Shift Base color Y axis", Float) = 1

           //v2.0.6
           _InteractMaxYoffset("Interaction max offset in Y axis", Float) = 1.5

		   //v1.9.9.2
		   //float shadowBrightnessCutoff;// = 0.1;
		   //float shadowBrightness;// = 1;
		   shadowBrightnessCutoff("shadow Brightness Cutoff (0 to 0.25)", Float) = 0 //disable the shadow brighting by default, use 0.1 for best result
		   shadowBrightness("shadow Brightness", Float) = 1

			   //v2.0.8
		   _InteractTexture("_Interact Texture", 2D) = "white" {}
		   _InteractTexturePos("Interact Texture Pos", Vector) = (0 ,5000, 0, 0)
			   //v2.1.1
			_shapeOnlyHeight("Shape Only Height",Float) = 0
			   //v2.1.12
		   _XTiles("X tiles",Float) = 1
		   _YTiles("Y tiles",Float) = 1

			_erasedShadowFactor("Shape Erased Grass Shadow Factor",Float) = 1

			   //v2.1.13
			   scaleYFactor("scale Y Factor",Vector) = (1 ,0, 1, 1)

			   //v1.7 - Local interact radial - amplitude - frequency v2.1.14
		   _InteractAmpFreqRad("_InteractAmpFreqRadial", Vector) = (1, 1, 1)
				_InteractPos2("Interact Position", Vector) = (0, 0, 0) //for lowering motion when interaction item is near

               //v1.8
               _localLightFactor("Control spot-point light effect",Float) = 1

               //MASK
               _maskTexture("Mask pattern Texture", 2D) = "white" {}
               PatternATilingOffset("PatternA Tiling Offset",Vector) = (1 ,1, 0, 0)
               patternAPower("patternA Power",Float) = 0
               patternGroundPower("pattern Ground Power",Float) = 0
               patternGroundHeight("pattern Ground Heigth",Float) = 0.1
               _shadowBrightFactor("Shadows Bright Factor",Float) = 0

               //v2.2
               useNoisePattern("use Noise Pattern",Float) = 0
               grassTypeHeights("texture sheet height per type", Vector) = (1, 1, 1, 1)

                   //v2.2a               
             _windTexture("Wind texture", 2D) = "white" {}
             _noiseWindParams("_noise Wind Params", Vector) = (1, 0, 1, 1)

             //v2.2b
             _ViewBendStrength("_ViewBendStrength",Float) = 0

    }
    SubShader {
        Tags {
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
        }
        Pass {
          /*  Name "ForwardBase"
            Tags {
                "LightMode"="ForwardBase"
            }*/

              Tags {
               "LightMode" = "UniversalForwardOnly"
           }

            Cull Off            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            //#define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_instancing //v1.7.8
            #pragma multi_compile_fwdbase_fullshadows
			#pragma multi_compile_fwdbase nolightmap //v4.1

        //v0.1a
//#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
//#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE 
//        #pragma multi_compile _ _FORWARD_PLUS

          #pragma multi_compile _ _ADDITIONAL_LIGHTS
        #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile _ _FORWARD_PLUS
        #pragma multi_compile _ SHADOWS_SHADOWMASK
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile_fragment _ _SHADOWS_SOFT
        #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile _ _LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile_fragment _ _LIGHT_COOKIES
        #pragma multi_compile_fragment _ _LIGHT_LAYERS

#if defined(_FORWARD_PLUS)
#define _ADDITIONAL_LIGHTS 1
#undef _ADDITIONAL_LIGHTS_VERTEX
#define USE_FORWARD_PLUS 1
#else
#define USE_FORWARD_PLUS 0
#endif

//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/D3D11.hlsl"

//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Clustering.hlsl"

//v0.2
// Should match: UnityEngine.Rendering.Universal + 1
#define SOFT_SHADOW_QUALITY_OFF    half(0.0)
#define SOFT_SHADOW_QUALITY_LOW    half(1.0)
#define SOFT_SHADOW_QUALITY_MEDIUM half(2.0)
#define SOFT_SHADOW_QUALITY_HIGH   half(3.0)

//#define USE_FORWARD_PLUS 1
#define USE_MY_ADDITIONAL_LIGHT_CALCULATE_SHADOWS
#define ADDITIONAL_LIGHT_CALCULATE_SHADOWS
#define MAIN_LIGHT_CALCULATE_SHADOWS
#define URP_FP_DISABLE_ZBINNING 0
#define URP_FP_DISABLE_TILING 0

//v0.1
 float4 _ScaledScreenParams;
    uniform float4 _ScaleBiasRt;
// ShadowParams
// x: ShadowStrength
// y: 1.0 if shadow is soft, 0.0 otherwise
float4x4    _MainLightWorldToShadow[4 + 1];
float4      _CascadeShadowSplitSpheres0;
float4      _CascadeShadowSplitSpheres1;
float4      _CascadeShadowSplitSpheres2;
float4      _CascadeShadowSplitSpheres3;
float4      _CascadeShadowSplitSphereRadii;
half ComputeCascadeIndex(float3 positionWS)
{
    float3 fromCenter0 = positionWS - _CascadeShadowSplitSpheres0.xyz;
    float3 fromCenter1 = positionWS - _CascadeShadowSplitSpheres1.xyz;
    float3 fromCenter2 = positionWS - _CascadeShadowSplitSpheres2.xyz;
    float3 fromCenter3 = positionWS - _CascadeShadowSplitSpheres3.xyz;
    float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

    half4 weights = half4(distances2 < _CascadeShadowSplitSphereRadii);
    weights.yzw = saturate(weights.yzw - weights.xyz);

    return half(4.0) - dot(weights, half4(4, 3, 2, 1));
}
float4      _MainLightShadowOffset0; // xy: offset0, zw: offset1
float4      _MainLightShadowOffset1; // xy: offset2, zw: offset3
float4      _MainLightShadowParams;   // (x: shadowStrength, y: >= 1.0 if soft shadows, 0.0 otherwise, z: main light fade scale, w: main light fade bias)
float4      _MainLightShadowmapSize;  // (xy: 1/width and 1/height, zw: width and height)
#define TEXTURE2D(textureName)                  Texture2D textureName
#define TEXTURE2D_SHADOW(textureName)         TEXTURE2D(textureName)
TEXTURE2D_SHADOW(_MainLightShadowmapTexture);
half4 GetMainLightShadowParams()
{
    return _MainLightShadowParams;
}
struct ShadowSamplingData
{
    half4 shadowOffset0;
    half4 shadowOffset1;
    float4 shadowmapSize;
    half softShadowQuality;
};
ShadowSamplingData GetMainLightShadowSamplingData()
{
    ShadowSamplingData shadowSamplingData;

    // shadowOffsets are used in SampleShadowmapFiltered for low quality soft shadows.
    shadowSamplingData.shadowOffset0 = _MainLightShadowOffset0;
    shadowSamplingData.shadowOffset1 = _MainLightShadowOffset1;

    // shadowmapSize is used in SampleShadowmapFiltered otherwise
    shadowSamplingData.shadowmapSize = _MainLightShadowmapSize;
    shadowSamplingData.softShadowQuality = _MainLightShadowParams.y;

    return shadowSamplingData;
}
#define SAMPLER_CMP(samplerName)              SamplerComparisonState samplerName
SAMPLER_CMP(sampler_LinearClampCompare);
#define TEXTURE2D_SHADOW_PARAM(textureName, samplerName)          TEXTURE2D(textureName),         SAMPLER_CMP(samplerName)
#define BEYOND_SHADOW_FAR(shadowCoord) shadowCoord.z <= 0.0 || shadowCoord.z >= 1.0
//float SampleShadowmapFiltered(TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap), float4 shadowCoord, ShadowSamplingData samplingData)
//{
//    float attenuation;
//
//#if defined(SHADER_API_MOBILE) || defined(SHADER_API_SWITCH)
//    // 4-tap hardware comparison
//    float4 attenuation4;
//    attenuation4.x = SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset0.xyz);
//    attenuation4.y = SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset1.xyz);
//    attenuation4.z = SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset2.xyz);
//    attenuation4.w = SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset3.xyz);
//    attenuation = dot(attenuation4, 0.25);
//#else
//    float fetchesWeights[9];
//    float2 fetchesUV[9];
//    SampleShadow_ComputeSamples_Tent_5x5(samplingData.shadowmapSize, shadowCoord.xy, fetchesWeights, fetchesUV);
//
//    attenuation = fetchesWeights[0] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[0].xy, shadowCoord.z));
//    attenuation += fetchesWeights[1] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[1].xy, shadowCoord.z));
//    attenuation += fetchesWeights[2] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[2].xy, shadowCoord.z));
//    attenuation += fetchesWeights[3] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[3].xy, shadowCoord.z));
//    attenuation += fetchesWeights[4] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[4].xy, shadowCoord.z));
//    attenuation += fetchesWeights[5] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[5].xy, shadowCoord.z));
//    attenuation += fetchesWeights[6] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[6].xy, shadowCoord.z));
//    attenuation += fetchesWeights[7] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[7].xy, shadowCoord.z));
//    attenuation += fetchesWeights[8] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[8].xy, shadowCoord.z));
//#endif
//
//    return attenuation;
//}
float SampleShadowmap(TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap), float4 shadowCoord, ShadowSamplingData samplingData, half4 shadowParams, bool isPerspectiveProjection = true)
{
    // Compiler will optimize this branch away as long as isPerspectiveProjection is known at compile time
    if (isPerspectiveProjection)
        shadowCoord.xyz /= shadowCoord.w;

    float attenuation;
    float shadowStrength = shadowParams.x;

    //// Quality levels are only for platforms requiring strict static branches
    //#if _SHADOWS_SOFT_LOW
    //    attenuation = SampleShadowmapFilteredLowQuality(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);
    //#elif _SHADOWS_SOFT_MEDIUM
    //    attenuation = SampleShadowmapFilteredMediumQuality(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);
    //#elif _SHADOWS_SOFT_HIGH
    //    attenuation = SampleShadowmapFilteredHighQuality(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);
    //#elif _SHADOWS_SOFT
    //    if (shadowParams.y > SOFT_SHADOW_QUALITY_OFF)
    //    {
    //        attenuation = SampleShadowmapFiltered(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);
    //    }
    //    else
    //    {
    //        attenuation = float(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz));
    //    }
    //#else
        attenuation = float(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz));
    //#endif

    attenuation = LerpWhiteTo(attenuation, shadowStrength);

    // Shadow coords that fall out of the light frustum volume must always return attenuation 1.0
    // TODO: We could use branch here to save some perf on some platforms.
    return BEYOND_SHADOW_FAR(shadowCoord) ? 1.0 : attenuation;
}
//#define TEXTURE2D_ARGS(textureName, samplerName) sampler2D textureName
//#define MAIN_LIGHT_CALCULATE_SHADOWS
half MainLightRealtimeShadow(float4 shadowCoord)
{
    #if !defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        return half(1.0);
    #elif defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
        return SampleScreenSpaceShadowmap(shadowCoord);
    #else
        ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
        half4 shadowParams = GetMainLightShadowParams();
        return SampleShadowmap(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_LinearClampCompare), shadowCoord, shadowSamplingData, shadowParams, false);
    #endif
}


//v0.1b
#if defined(UNITY_DOTS_INSTANCING_ENABLED) && !defined(USE_LEGACY_LIGHTMAPS)
// ^ GPU-driven rendering is enabled, and we haven't opted-out from lightmap
// texture arrays. This minimizes batch breakages, but texture arrays aren't
// supported in a performant way on all GPUs.
#define SHADOWMASK_NAME unity_ShadowMasks
#define SHADOWMASK_SAMPLER_NAME samplerunity_ShadowMasks
#define SHADOWMASK_SAMPLE_EXTRA_ARGS , unity_LightmapIndex.x
#else
// ^ Lightmaps are not bound as texture arrays, but as individual textures. The
// batch is broken every time lightmaps are changed, but this is well-supported
// on all GPUs.
#define SHADOWMASK_NAME unity_ShadowMask
#define SHADOWMASK_SAMPLER_NAME samplerunity_ShadowMask
#define SHADOWMASK_SAMPLE_EXTRA_ARGS
#endif

#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
#define SAMPLE_SHADOWMASK(uv) SAMPLE_TEXTURE2D_LIGHTMAP(SHADOWMASK_NAME, SHADOWMASK_SAMPLER_NAME, uv SHADOWMASK_SAMPLE_EXTRA_ARGS);
#elif !defined (LIGHTMAP_ON)
#define SAMPLE_SHADOWMASK(uv) unity_ProbesOcclusion;
#else
#define SAMPLE_SHADOWMASK(uv) half4(1, 1, 1, 1);
#endif

#ifdef LIGHTMAP_ON
#define DECLARE_LIGHTMAP_OR_SH(lmName, shName, index) float2 lmName : TEXCOORD##index
#define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT) OUT.xy = lightmapUV.xy * lightmapScaleOffset.xy + lightmapScaleOffset.zw;
#define OUTPUT_SH(normalWS, OUT)
#else
#define DECLARE_LIGHTMAP_OR_SH(lmName, shName, index) half3 shName : TEXCOORD##index
#define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT)
#define OUTPUT_SH(normalWS, OUT) OUT.xyz = SampleSHVertex(normalWS)
#endif
 void Shadowmask_half (float2 lightmapUV, out half4 Shadowmask){
#ifdef SHADERGRAPH_PREVIEW
Shadowmask = half4(1, 1, 1, 1);
#else
OUTPUT_LIGHTMAP_UV(lightmapUV, unity_LightmapST, lightmapUV);
Shadowmask = SAMPLE_SHADOWMASK(lightmapUV);
#endif
}
//Graphics/Packages/com.unity.render-pipelines.core/ShaderLibrary SpaceTransforms.hlsl
// Transforms position from world space to homogenous space
// Transform to homogenous clip space
float4x4 GetWorldToHClipMatrix()
{
    return UNITY_MATRIX_VP;
}
float4 TransformWorldToHClip(float3 positionWS)
{
    return mul(GetWorldToHClipMatrix(), float4(positionWS, 1.0));
}
 struct InputData
{
    float3  positionWS;
    float4  positionCS;
    float3  normalWS;
    half3   viewDirectionWS;
    float4  shadowCoord;
    half    fogCoord;
    half3   vertexLighting;
    half3   bakedGI;
    float2  normalizedScreenSpaceUV;
    half4   shadowMask;
    half3x3 tangentToWorld;

#if defined(DEBUG_DISPLAY)
    half2   dynamicLightmapUV;
    half2   staticLightmapUV;
    float3  vertexSH;

    half3 brdfDiffuse;
    half3 brdfSpecular;

    // Mipmap Streaming Debug
    float2 uv;
    uint mipCount;

    // texelSize :
    // x = 1 / width
    // y = 1 / height
    // z = width
    // w = height
    float4 texelSize;

    // mipInfo :
    // x = quality settings minStreamingMipLevel
    // y = original mip count for texture
    // z = desired on screen mip level
    // w = loaded mip level
    float4 mipInfo;

    // streamInfo :
    // x = streaming priority
    // y = time stamp of the latest texture upload
    // z = streaming status
    // w = 0
    float4 streamInfo;

    float3 originalColor;
#endif
};
//Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl
half4 unity_LightData;
half4 unity_LightIndices[2];
half4 _AdditionalLightsCount;
int GetAdditionalLightsCount()
{
#if USE_FORWARD_PLUS//#if USE_CLUSTERED_LIGHTING
    // Counting the number of lights in clustered requires traversing the bit list, and is not needed up front.
    return 0;
#else
    // TODO: we need to expose in SRP api an ability for the pipeline cap the amount of lights
    // in the culling. This way we could do the loop branch with an uniform
    // This would be helpful to support baking exceeding lights in SH as well
    return int(min(_AdditionalLightsCount.x, unity_LightData.y));
#endif
}

//Graphics/Packages/com.unity.render-pipelines.universal/ShaderLibrary/ RealtimeLights.hlsl
// Abstraction over Light shading data.
struct Light
{
    half3   direction;
    half3   color;
    float   distanceAttenuation; // full-float precision required on some platforms
    half    shadowAttenuation;
    uint    layerMask;
};



//v0.2a
//input.hlsl
#define HALF_MIN 6.103515625e-5  // 2^-14, the same value for 10, 11 and 16-bit: https://www.khronos.org/opengl/wiki/Sm 
#define HALF_MIN_SQRT 0.0078125  // 2^-7 == sqrt(HALF_MIN), useful for ensuring HALF_MIN after x^2
#define MAX_VISIBLE_LIGHT_COUNT_LOW_END_MOBILE (16)
#define MAX_VISIBLE_LIGHT_COUNT_MOBILE (32)
#define MAX_VISIBLE_LIGHT_COUNT_DESKTOP (256)
// Must match: UniversalRenderPipeline.maxVisibleAdditionalLights
#if defined(SHADER_API_MOBILE) && defined(SHADER_API_GLES30)
#define MAX_VISIBLE_LIGHTS MAX_VISIBLE_LIGHT_COUNT_LOW_END_MOBILE
// WebGPU's minimal limits are based on mobile rather than desktop, so it will need to assume mobile.
#elif defined(SHADER_API_MOBILE) || (defined(SHADER_API_GLCORE) && !defined(SHADER_API_SWITCH)) || defined(SHADER_API_GLES3) || defined(SHADER_API_WEBGPU) // Workaround because SHADER_API_GLCORE is also defined when SHADER_API_SWITCH is
#define MAX_VISIBLE_LIGHTS MAX_VISIBLE_LIGHT_COUNT_MOBILE
#else
#define MAX_VISIBLE_LIGHTS MAX_VISIBLE_LIGHT_COUNT_DESKTOP
#endif



#if USE_FORWARD_PLUS && defined(LIGHTMAP_ON) && defined(LIGHTMAP_SHADOW_MIXING)
#define FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK if (_AdditionalLightsColor[lightIndex].a > 0.0h) continue;
#else
#define FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
#endif
#if USE_FORWARD_PLUS
float4 _FPParams0;
float4 _FPParams1;
float4 _FPParams2;
#define URP_FP_ZBIN_SCALE (_FPParams0.x)
#define URP_FP_ZBIN_OFFSET (_FPParams0.y)
#define URP_FP_PROBES_BEGIN ((uint)_FPParams0.z)
// Directional lights would be in all clusters, so they don't go into the cluster structure.
// Instead, they are stored first in the light buffer.
#define URP_FP_DIRECTIONAL_LIGHTS_COUNT ((uint)_FPParams0.w)
// Scale from screen-space UV [0, 1] to tile coordinates [0, tile resolution].
#define URP_FP_TILE_SCALE ((float2)_FPParams1.xy)
#define URP_FP_TILE_COUNT_X ((uint)_FPParams1.z)
#define URP_FP_WORDS_PER_TILE ((uint)_FPParams1.w)
#define URP_FP_ZBIN_COUNT ((uint)_FPParams2.x)
#define URP_FP_TILE_COUNT ((uint)_FPParams2.y)
#endif

float4x4 GetWorldToViewMatrix()
{
    return UNITY_MATRIX_V;
}
float3 GetCameraPositionWS()
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    return 0;
#endif
    return _WorldSpaceCameraPos;
}
// Returns 'true' if the current view performs a perspective projection.
bool IsPerspectiveProjection()
{
#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_SHADOWS)
    return (unity_OrthoParams.w == 0);
#else
    // TODO: set 'unity_OrthoParams' during the shadow pass.
    return UNITY_MATRIX_P[3][3] == 0;
#endif
}
// Returns the forward (central) direction of the current view in the world space.
float3 GetViewForwardDir()
{
    float4x4 viewMat = GetWorldToViewMatrix();
    return -viewMat[2].xyz;
}
// Match with values in UniversalRenderPipeline.cs
#define MAX_ZBIN_VEC4S 1024
#if MAX_VISIBLE_LIGHTS <= 16
#define MAX_LIGHTS_PER_TILE 32
#define MAX_TILE_VEC4S 1024
#define MAX_REFLECTION_PROBES 16
#elif MAX_VISIBLE_LIGHTS <= 32
#define MAX_LIGHTS_PER_TILE 32
#define MAX_TILE_VEC4S 1024
#define MAX_REFLECTION_PROBES 32
#else
#define MAX_LIGHTS_PER_TILE MAX_VISIBLE_LIGHTS
#define MAX_TILE_VEC4S 4096
#define MAX_REFLECTION_PROBES 64
#endif
CBUFFER_START(urp_ZBinBuffer)
float4 urp_ZBins[MAX_ZBIN_VEC4S];
CBUFFER_END
CBUFFER_START(urp_TileBuffer)
float4 urp_Tiles[MAX_TILE_VEC4S];
CBUFFER_END
// Select uint4 component by index.
// Helper to improve codegen for 2d indexing (data[x][y])
// Replace:
// data[i / 4][i % 4];
// with:
// select4(data[i / 4], i % 4);
uint Select4(uint4 v, uint i)
{
    // x = 0 = 00
    // y = 1 = 01
    // z = 2 = 10
    // w = 3 = 11
    uint mask0 = uint(int(i << 31) >> 31);
    uint mask1 = uint(int(i << 30) >> 31);
    return
        (((v.w & mask0) | (v.z & ~mask0)) & mask1) |
        (((v.y & mask0) | (v.x & ~mask0)) & ~mask1);
}
#if SHADER_TARGET < 45
uint URP_FirstBitLow(uint m)
{
    // http://graphics.stanford.edu/~seander/bithacks.html#ZerosOnRightFloatCast
    return (asuint((float)(m & asuint(-asint(m)))) >> 23) - 0x7F;
}
#define FIRST_BIT_LOW URP_FirstBitLow
#else
#define FIRST_BIT_LOW firstbitlow
#endif

#if USE_FORWARD_PLUS
#if (!defined(UNITY_COMPILER_DXC) && (defined(UNITY_PLATFORM_OSX) || defined(UNITY_PLATFORM_IOS) || defined(UNITY_PLATFORM_VISIONOS)) && defined(SHADER_API_METAL)) || defined(SHADER_API_PS5)

#define SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER 1

// On Metal Foveated Rendering is currently not supported with DXC
#pragma warning (disable : 3568) // unknown pragma ignored

#pragma never_use_dxc metal
#pragma dynamic_branch _ _FOVEATED_RENDERING_NON_UNIFORM_RASTER

#pragma warning (default : 3568) // restore unknown pragma ignored

#endif
//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
// Debug switches for disabling parts of the algorithm. Not implemented for mobile.
//#define URP_FP_DISABLE_ZBINNING 0
//#define URP_FP_DISABLE_TILING 0

// internal
struct ClusterIterator
{
    uint tileOffset;
    uint zBinOffset;
    uint tileMask;
    // Stores the next light index in first 16 bits, and the max light index in the last 16 bits.
    uint entityIndexNextMax;
};

// internal
ClusterIterator ClusterInit(float2 normalizedScreenSpaceUV, float3 positionWS, int headerIndex)
{
    ClusterIterator state = (ClusterIterator)0;

#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
#if UNITY_UV_STARTS_AT_TOP
        // RemapFoveatedRenderingNonUniformToLinear expects the UV coordinate to be non-flipped, so we un-flip it before
        // the call, and then flip it back afterwards.
        normalizedScreenSpaceUV.y = 1.0 - normalizedScreenSpaceUV.y;
#endif
        normalizedScreenSpaceUV = RemapFoveatedRenderingNonUniformToLinear(normalizedScreenSpaceUV);
#if UNITY_UV_STARTS_AT_TOP
        normalizedScreenSpaceUV.y = 1.0 - normalizedScreenSpaceUV.y;
#endif
    }
#endif // SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER

    uint2 tileId = uint2(normalizedScreenSpaceUV * URP_FP_TILE_SCALE);
    state.tileOffset = tileId.y * URP_FP_TILE_COUNT_X + tileId.x;
#if defined(USING_STEREO_MATRICES)
    state.tileOffset += URP_FP_TILE_COUNT * unity_StereoEyeIndex;
#endif
    state.tileOffset *= URP_FP_WORDS_PER_TILE;

    float viewZ = dot(GetViewForwardDir(), positionWS - GetCameraPositionWS());
    uint zBinBaseIndex = (uint)((IsPerspectiveProjection() ? log2(viewZ) : viewZ) * URP_FP_ZBIN_SCALE + URP_FP_ZBIN_OFFSET);
#if defined(USING_STEREO_MATRICES)
    zBinBaseIndex += URP_FP_ZBIN_COUNT * unity_StereoEyeIndex;
#endif
    // The Zbin buffer is laid out in the following manner:
    //                          ZBin 0                                      ZBin 1
    //  .-------------------------^------------------------. .----------------^-------
    // | header0 | header1 | word 1 | word 2 | ... | word N | header0 | header 1 | ...
    //                     `----------------v--------------'
    //                            URP_FP_WORDS_PER_TILE
    //
    // The total length of this buffer is `4*MAX_ZBIN_VEC4S`. `zBinBaseIndex` should
    // always point to the `header 0` of a ZBin, so we clamp it accordingly, to
    // avoid out-of-bounds indexing of the ZBin buffer.
    zBinBaseIndex = zBinBaseIndex * (2 + URP_FP_WORDS_PER_TILE);
    zBinBaseIndex = min(zBinBaseIndex, 4 * MAX_ZBIN_VEC4S - (2 + URP_FP_WORDS_PER_TILE));

    uint zBinHeaderIndex = zBinBaseIndex + headerIndex;
    state.zBinOffset = zBinBaseIndex + 2;

#if !URP_FP_DISABLE_ZBINNING
    uint header = Select4(asuint(urp_ZBins[zBinHeaderIndex / 4]), zBinHeaderIndex % 4);
#else
    uint header = headerIndex == 0 ? ((URP_FP_PROBES_BEGIN - 1) << 16) : (((URP_FP_WORDS_PER_TILE * 32 - 1) << 16) | URP_FP_PROBES_BEGIN);
#endif
#if MAX_LIGHTS_PER_TILE > 32 || !defined(_ENVIRONMENTREFLECTIONS_OFF)
    state.entityIndexNextMax = header;
#else
    uint tileIndex = state.tileOffset;
    uint zBinIndex = state.zBinOffset;
    if (URP_FP_WORDS_PER_TILE > 0)
    {
        state.tileMask =
            Select4(asuint(urp_Tiles[tileIndex / 4]), tileIndex % 4) &
            Select4(asuint(urp_ZBins[zBinIndex / 4]), zBinIndex % 4) &
            (0xFFFFFFFFu << (header & 0x1F)) & (0xFFFFFFFFu >> (31 - (header >> 16)));
    }
#endif

    return state;
}

// internal
bool ClusterNext(inout ClusterIterator it, out uint entityIndex)
{
#if MAX_LIGHTS_PER_TILE > 32 || !defined(_ENVIRONMENTREFLECTIONS_OFF)
    uint maxIndex = it.entityIndexNextMax >> 16;
    [loop] while (it.tileMask == 0 && (it.entityIndexNextMax & 0xFFFF) <= maxIndex)
    {
        // Extract the lower 16 bits and shift by 5 to divide by 32.
        uint wordIndex = ((it.entityIndexNextMax & 0xFFFF) >> 5);
        uint tileIndex = it.tileOffset + wordIndex;
        uint zBinIndex = it.zBinOffset + wordIndex;
        it.tileMask =
#if !URP_FP_DISABLE_TILING
            Select4(asuint(urp_Tiles[tileIndex / 4]), tileIndex % 4) &
#endif
#if !URP_FP_DISABLE_ZBINNING
            Select4(asuint(urp_ZBins[zBinIndex / 4]), zBinIndex % 4) &
#endif
            // Mask out the beginning and end of the word.
            (0xFFFFFFFFu << (it.entityIndexNextMax & 0x1F)) & (0xFFFFFFFFu >> (31 - min(31, maxIndex - wordIndex * 32)));
        // The light index can start at a non-multiple of 32, but the following iterations should always be multiples of 32.
        // So we add 32 and mask out the lower bits.
        it.entityIndexNextMax = (it.entityIndexNextMax + 32) & ~31;
    }
#endif
    bool hasNext = it.tileMask != 0;
    uint bitIndex = FIRST_BIT_LOW(it.tileMask);
    it.tileMask ^= (1 << bitIndex);
#if MAX_LIGHTS_PER_TILE > 32 || !defined(_ENVIRONMENTREFLECTIONS_OFF)
    // Subtract 32 because it stores the index of the _next_ word to fetch, but we want the current.
    // The upper 16 bits and bits representing values < 32 are masked out. The latter is due to the fact that it will be
    // included in what FIRST_BIT_LOW returns.
    entityIndex = (((it.entityIndexNextMax - 32) & (0xFFFF & ~31))) + bitIndex;
#else
    entityIndex = bitIndex;
#endif
    return hasNext;
}
#endif






#if USE_FORWARD_PLUS
#define LIGHT_LOOP_BEGIN(lightCount) { \
    uint lightIndex; \
    ClusterIterator _urp_internal_clusterIterator = ClusterInit(inputData.normalizedScreenSpaceUV, inputData.positionWS, 0); \
    [loop] while (ClusterNext(_urp_internal_clusterIterator, lightIndex)) { \
        lightIndex += URP_FP_DIRECTIONAL_LIGHTS_COUNT; \
        if (lightIndex > MAX_VISIBLE_LIGHTS) { break; } \
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK       
#define LIGHT_LOOP_END } }
#else
#define LIGHT_LOOP_BEGIN(lightCount) \
    for (uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex) {
#define LIGHT_LOOP_END }
#endif
/////

///////////////////////////////////////////////////////////////////////////////
//                        Attenuation Functions                               /
///////////////////////////////////////////////////////////////////////////////

// Matches Unity Vanilla HINT_NICE_QUALITY attenuation
// Attenuation smoothly decreases to light range.
float DistanceAttenuation(float distanceSqr, half2 distanceAttenuation)
{
    // We use a shared distance attenuation for additional directional and puctual lights
    // for directional lights attenuation will be 1
    float lightAtten = rcp(distanceSqr);
    float2 distanceAttenuationFloat = float2(distanceAttenuation);

    // Use the smoothing factor also used in the Unity lightmapper.
    half factor = half(distanceSqr * distanceAttenuationFloat.x);
    half smoothFactor = saturate(half(1.0) - factor * factor);
    smoothFactor = smoothFactor * smoothFactor;

    return lightAtten * smoothFactor;
}

half AngleAttenuation(half3 spotDirection, half3 lightDirection, half2 spotAttenuation)
{
    // Spot Attenuation with a linear falloff can be defined as
    // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
    // This can be rewritten as
    // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
    // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
    // SdotL * spotAttenuation.x + spotAttenuation.y

    // If we precompute the terms in a MAD instruction
    half SdotL = dot(spotDirection, lightDirection);
    half atten = saturate(SdotL * spotAttenuation.x + spotAttenuation.y);
    return atten * atten;
}



#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
StructuredBuffer<LightData> _AdditionalLightsBuffer;
StructuredBuffer<int> _AdditionalLightsIndices;
#else
// GLES3 causes a performance regression in some devices when using CBUFFER.
#ifndef SHADER_API_GLES3
CBUFFER_START(AdditionalLights)
#endif
float4 _AdditionalLightsPosition[MAX_VISIBLE_LIGHTS];
// In Forward+, .a stores whether the light is using subtractive mixed mode.
half4 _AdditionalLightsColor[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsAttenuation[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsSpotDir[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsOcclusionProbes[MAX_VISIBLE_LIGHTS];
float _AdditionalLightsLayerMasks[MAX_VISIBLE_LIGHTS]; // we want uint[] but Unity api does not support it.
#ifndef SHADER_API_GLES3
CBUFFER_END
#endif
#endif

// Fills a light struct given a perObjectLightIndex
Light GetAdditionalPerObjectLight(int perObjectLightIndex, float3 positionWS)
{
    // Abstraction over Light input constants
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    float4 lightPositionWS = _AdditionalLightsBuffer[perObjectLightIndex].position;
    half3 color = _AdditionalLightsBuffer[perObjectLightIndex].color.rgb;
    half4 distanceAndSpotAttenuation = _AdditionalLightsBuffer[perObjectLightIndex].attenuation;
    half4 spotDirection = _AdditionalLightsBuffer[perObjectLightIndex].spotDirection;
    uint lightLayerMask = _AdditionalLightsBuffer[perObjectLightIndex].layerMask;
#else
    float4 lightPositionWS = _AdditionalLightsPosition[perObjectLightIndex];
    half3 color = _AdditionalLightsColor[perObjectLightIndex].rgb;
    half4 distanceAndSpotAttenuation = _AdditionalLightsAttenuation[perObjectLightIndex];
    half4 spotDirection = _AdditionalLightsSpotDir[perObjectLightIndex];
    uint lightLayerMask = asuint(_AdditionalLightsLayerMasks[perObjectLightIndex]);
#endif

    // Directional lights store direction in lightPosition.xyz and have .w set to 0.0.
    // This way the following code will work for both directional and punctual lights.
    float3 lightVector = lightPositionWS.xyz - positionWS * lightPositionWS.w;
    float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);

    half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));
    // full-float precision required on some platforms
    float attenuation = DistanceAttenuation(distanceSqr, distanceAndSpotAttenuation.xy) * AngleAttenuation(spotDirection.xyz, lightDirection, distanceAndSpotAttenuation.zw);

    Light light;
    light.direction = lightDirection;
    light.distanceAttenuation = attenuation;
    light.shadowAttenuation = 1.0; // This value can later be overridden in GetAdditionalLight(uint i, float3 positionWS, half4 shadowMask)
    light.color = color;
    light.layerMask = lightLayerMask;

    return light;
}
uint GetPerObjectLightIndexOffset()
{
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    return uint(unity_LightData.x);
#else
    return 0;
#endif
}
// Returns a per-object index given a loop index.
// This abstract the underlying data implementation for storing lights/light indices
int GetPerObjectLightIndex(uint index)
{
    /////////////////////////////////////////////////////////////////////////////////////////////
    // Structured Buffer Path                                                                   /
    //                                                                                          /
    // Lights and light indices are stored in StructuredBuffer. We can just index them.         /
    // Currently all non-mobile platforms take this path :(                                     /
    // There are limitation in mobile GPUs to use SSBO (performance / no vertex shader support) /
    /////////////////////////////////////////////////////////////////////////////////////////////
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    uint offset = uint(unity_LightData.x);
    return _AdditionalLightsIndices[offset + index];

    /////////////////////////////////////////////////////////////////////////////////////////////
    // UBO path                                                                                 /
    //                                                                                          /
    // We store 8 light indices in half4 unity_LightIndices[2];                                 /
    // Due to memory alignment unity doesn't support int[] or float[]                           /
    // Even trying to reinterpret cast the unity_LightIndices to float[] won't work             /
    // it will cast to float4[] and create extra register pressure. :(                          /
    /////////////////////////////////////////////////////////////////////////////////////////////
#else
    // since index is uint shader compiler will implement
    // div & mod as bitfield ops (shift and mask).

    // TODO: Can we index a float4? Currently compiler is
    // replacing unity_LightIndicesX[i] with a dp4 with identity matrix.
    // u_xlat16_40 = dot(unity_LightIndices[int(u_xlatu13)], ImmCB_0_0_0[u_xlati1]);
    // This increases both arithmetic and register pressure.
    //
    // NOTE: min16float4 bug workaround.
    // Take the "vec4" part into float4 tmp variable in order to force float4 math.
    // It appears indexing half4 as min16float4 on DX11 can fail. (dp4 {min16f})
    float4 tmp = unity_LightIndices[index / 4];
    return int(tmp[index % 4]);
#endif
}

// Fills a light struct given a loop i index. This will convert the i
// index to a perObjectLightIndex
// returns 0.0 if position is in light's shadow
// returns 1.0 if position is in light
half MixRealtimeAndBakedShadows(half realtimeShadow, half bakedShadow, half shadowFade)
{
#if defined(LIGHTMAP_SHADOW_MIXING)
    return min(lerp(realtimeShadow, 1, shadowFade), bakedShadow);
#else
    return lerp(realtimeShadow, bakedShadow, shadowFade);
#endif
}

//#define ADDITIONAL_LIGHT_CALCULATE_SHADOWS


#if defined(ADDITIONAL_LIGHT_CALCULATE_SHADOWS) && defined(USE_MY_ADDITIONAL_LIGHT_CALCULATE_SHADOWS)
#if !USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
// Point lights can use 6 shadow slices. Some mobile GPUs performance decrease drastically with uniform
// blocks bigger than 8kb while others have a 64kb max uniform block size. This number ensures size of buffer
// AdditionalLightShadows stays reasonable. It also avoids shader compilation errors on SHADER_API_GLES30
// devices where max number of uniforms per shader GL_MAX_FRAGMENT_UNIFORM_VECTORS is low (224)
float4      _AdditionalShadowParams[MAX_VISIBLE_LIGHTS];         // Per-light data
float4x4    _AdditionalLightsWorldToShadow[MAX_VISIBLE_LIGHTS];  // Per-shadow-slice-data
#endif
#endif
float4      _AdditionalShadowOffset0; // xy: offset0, zw: offset1
float4      _AdditionalShadowOffset1; // xy: offset2, zw: offset3
float4      _AdditionalShadowFadeParams; // x: additional light fade scale, y: additional light fade bias, z: 0.0, w: 0.0)
float4      _AdditionalShadowmapSize; // (xy: 1/width and 1/height, zw: width and height)

// ShadowParams
// x: ShadowStrength
// y: >= 1.0 if shadow is soft, 0.0 otherwise. Higher value for higher quality. (1.0 == low, 2.0 == medium, 3.0 == high)
// z: 1.0 if cast by a point light (6 shadow slices), 0.0 if cast by a spot light (1 shadow slice)
// w: first shadow slice index for this light, there can be 6 in case of point lights. (-1 for non-shadow-casting-lights)
half4 GetAdditionalLightShadowParams(int lightIndex)
{
    half4 results;
#if defined(ADDITIONAL_LIGHT_CALCULATE_SHADOWS) && defined(USE_MY_ADDITIONAL_LIGHT_CALCULATE_SHADOWS)
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    results = _AdditionalShadowParams_SSBO[lightIndex];
#else
    results = _AdditionalShadowParams[lightIndex];
    results.w = lightIndex < 0 ? -1 : results.w;
#endif
#else
    // Same defaults as set in AdditionalLightsShadowCasterPass.cs
    return half4(0, 0, 0, -1);
#endif

    return results;
}

ShadowSamplingData GetAdditionalLightShadowSamplingData(int index)
{
    ShadowSamplingData shadowSamplingData = (ShadowSamplingData)0;

#if defined(ADDITIONAL_LIGHT_CALCULATE_SHADOWS) && defined(USE_MY_ADDITIONAL_LIGHT_CALCULATE_SHADOWS)
    // shadowOffsets are used in SampleShadowmapFiltered for low quality soft shadows.
    shadowSamplingData.shadowOffset0 = _AdditionalShadowOffset0;
    shadowSamplingData.shadowOffset1 = _AdditionalShadowOffset1;

    // shadowmapSize is used in SampleShadowmapFiltered otherwise.
    shadowSamplingData.shadowmapSize = _AdditionalShadowmapSize;
    shadowSamplingData.softShadowQuality = _AdditionalShadowParams[index].y;
#endif

    return shadowSamplingData;
}
#define CUBEMAPFACE_POSITIVE_X 0
#define CUBEMAPFACE_NEGATIVE_X 1
#define CUBEMAPFACE_POSITIVE_Y 2
#define CUBEMAPFACE_NEGATIVE_Y 3
#define CUBEMAPFACE_POSITIVE_Z 4
#define CUBEMAPFACE_NEGATIVE_Z 5

#ifndef INTRINSIC_CUBEMAP_FACE_ID
float CubeMapFaceID(float3 dir)
{
    float faceID;

    if (abs(dir.z) >= abs(dir.x) && abs(dir.z) >= abs(dir.y))
    {
        faceID = (dir.z < 0.0) ? CUBEMAPFACE_NEGATIVE_Z : CUBEMAPFACE_POSITIVE_Z;
    }
    else if (abs(dir.y) >= abs(dir.x))
    {
        faceID = (dir.y < 0.0) ? CUBEMAPFACE_NEGATIVE_Y : CUBEMAPFACE_POSITIVE_Y;
    }
    else
    {
        faceID = (dir.x < 0.0) ? CUBEMAPFACE_NEGATIVE_X : CUBEMAPFACE_POSITIVE_X;
    }

    return faceID;
}
#endif // INTRINSIC_CUBEMAP_FACE_ID
TEXTURE2D_SHADOW(_AdditionalLightsShadowmapTexture);
half AdditionalLightRealtimeShadow(int lightIndex, float3 positionWS, half3 lightDirection)
{
#if defined(ADDITIONAL_LIGHT_CALCULATE_SHADOWS) && defined(USE_MY_ADDITIONAL_LIGHT_CALCULATE_SHADOWS)
    ShadowSamplingData shadowSamplingData = GetAdditionalLightShadowSamplingData(lightIndex);

    half4 shadowParams = GetAdditionalLightShadowParams(lightIndex);

    int shadowSliceIndex = shadowParams.w;
    if (shadowSliceIndex < 0)
        return 1.0;

    half isPointLight = shadowParams.z;

    UNITY_BRANCH
        if (isPointLight)
        {
            // This is a point light, we have to find out which shadow slice to sample from
            float cubemapFaceId = CubeMapFaceID(-lightDirection);
            shadowSliceIndex += cubemapFaceId;
        }

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    float4 shadowCoord = mul(_AdditionalLightsWorldToShadow_SSBO[shadowSliceIndex], float4(positionWS, 1.0));
#else
    float4 shadowCoord = mul(_AdditionalLightsWorldToShadow[shadowSliceIndex], float4(positionWS, 1.0));
#endif

    return SampleShadowmap(TEXTURE2D_ARGS(_AdditionalLightsShadowmapTexture, sampler_LinearClampCompare), shadowCoord, shadowSamplingData, shadowParams, true);
#else
    return half(1.0);
#endif
}
half GetAdditionalLightShadowFade(float3 positionWS)
{
#if defined(ADDITIONAL_LIGHT_CALCULATE_SHADOWS) && defined(USE_MY_ADDITIONAL_LIGHT_CALCULATE_SHADOWS)
    float3 camToPixel = positionWS - _WorldSpaceCameraPos;
    float distanceCamToPixel2 = dot(camToPixel, camToPixel);

    float fade = saturate(distanceCamToPixel2 * float(_AdditionalShadowFadeParams.x) + float(_AdditionalShadowFadeParams.y));
    return half(fade);
#else
    return half(1.0);
#endif
}
half AdditionalLightShadow(int lightIndex, float3 positionWS, half3 lightDirection, half4 shadowMask, half4 occlusionProbeChannels)
{
    half realtimeShadow = AdditionalLightRealtimeShadow(lightIndex, positionWS, lightDirection);

#ifdef CALCULATE_BAKED_SHADOWS
    half bakedShadow = BakedShadow(shadowMask, occlusionProbeChannels);
#else
    half bakedShadow = half(1.0);
#endif

#if defined(ADDITIONAL_LIGHT_CALCULATE_SHADOWS) && defined(USE_MY_ADDITIONAL_LIGHT_CALCULATE_SHADOWS)//#ifdef ADDITIONAL_LIGHT_CALCULATE_SHADOWS
    half shadowFade = GetAdditionalLightShadowFade(positionWS);
#else
    half shadowFade = half(1.0);
#endif

    return MixRealtimeAndBakedShadows(realtimeShadow, bakedShadow, shadowFade);
}

Light GetAdditionalLight(uint i, float3 positionWS)
{
    #if USE_FORWARD_PLUS
        int lightIndex = i;
    #else
        int lightIndex = GetPerObjectLightIndex(i);
    #endif
    return GetAdditionalPerObjectLight(lightIndex, positionWS);
}




//COOKIES

//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LightCookie/LightCookieInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LightCookie/LightCookieTypes.hlsl"

// Textures
TEXTURE2D(_MainLightCookieTexture);
TEXTURE2D(_AdditionalLightsCookieAtlasTexture);

// Samplers
SAMPLER(sampler_MainLightCookieTexture);

// Buffers
// GLES3 causes a performance regression in some devices when using CBUFFER.
#ifndef LIGHT_SHADOWS_NO_CBUFFER
CBUFFER_START(LightCookies)
#endif
float4x4 _MainLightWorldToLight;
float _AdditionalLightsCookieEnableBits[(MAX_VISIBLE_LIGHTS + 31) / 32];
float _MainLightCookieTextureFormat;
float _AdditionalLightsCookieAtlasTextureFormat;
#if !USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
float4x4 _AdditionalLightsWorldToLights[MAX_VISIBLE_LIGHTS];
float4 _AdditionalLightsCookieAtlasUVRects[MAX_VISIBLE_LIGHTS]; // (xy: uv size, zw: uv offset)
float _AdditionalLightsLightTypes[MAX_VISIBLE_LIGHTS];
#endif
#ifndef LIGHT_SHADOWS_NO_CBUFFER
CBUFFER_END
#endif

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
StructuredBuffer<float4x4> _AdditionalLightsWorldToLightBuffer;
StructuredBuffer<float4>   _AdditionalLightsCookieAtlasUVRectBuffer; // UV rect into light cookie atlas (xy: uv offset, zw: uv size)
StructuredBuffer<float>    _AdditionalLightsLightTypeBuffer;
#endif

// Data Getters

float4x4 GetLightCookieWorldToLightMatrix(int lightIndex)
{
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    return _AdditionalLightsWorldToLightBuffer[lightIndex];
#else
    return _AdditionalLightsWorldToLights[lightIndex];
#endif
}

float4 GetLightCookieAtlasUVRect(int lightIndex)
{
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    return _AdditionalLightsCookieAtlasUVRectBuffer[lightIndex];
#else
    return _AdditionalLightsCookieAtlasUVRects[lightIndex];
#endif
}

int GetLightCookieLightType(int lightIndex)
{
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    return _AdditionalLightsLightTypeBuffer[lightIndex];
#else
    return _AdditionalLightsLightTypes[lightIndex];
#endif
}

bool IsMainLightCookieTextureRGBFormat()
{
    return _MainLightCookieTextureFormat == URP_LIGHT_COOKIE_FORMAT_RGB;
}

bool IsMainLightCookieTextureAlphaFormat()
{
    return _MainLightCookieTextureFormat == URP_LIGHT_COOKIE_FORMAT_ALPHA;
}

bool IsAdditionalLightsCookieAtlasTextureRGBFormat()
{
    return _AdditionalLightsCookieAtlasTextureFormat == URP_LIGHT_COOKIE_FORMAT_RGB;
}

bool IsAdditionalLightsCookieAtlasTextureAlphaFormat()
{
    return _AdditionalLightsCookieAtlasTextureFormat == URP_LIGHT_COOKIE_FORMAT_ALPHA;
}

// Sampling

float4 SampleMainLightCookieTexture(float2 uv)
{
    return SAMPLE_TEXTURE2D(_MainLightCookieTexture, sampler_MainLightCookieTexture, uv);
}

float4 SampleAdditionalLightsCookieAtlasTexture(float2 uv)
{
    // No mipmap support
    return SAMPLE_TEXTURE2D_LOD(_AdditionalLightsCookieAtlasTexture, sampler_LinearClamp, uv, 0);
}

// Helpers
bool IsMainLightCookieEnabled()
{
    return _MainLightCookieTextureFormat != URP_LIGHT_COOKIE_FORMAT_NONE;
}

bool IsLightCookieEnabled(int lightBufferIndex)
{
#if 0
    float4 uvRect = GetLightCookieAtlasUVRect(lightBufferIndex);
    return any(uvRect != 0);
#else
    // 2^5 == 32, bit mask for a float/uint.
    uint elemIndex = ((uint)lightBufferIndex) >> 5;
    uint bitOffset = (uint)lightBufferIndex & ((1 << 5) - 1);

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    uint elem = asuint(_AdditionalLightsCookieEnableBitsBuffer[elemIndex]);
#else
    uint elem = asuint(_AdditionalLightsCookieEnableBits[elemIndex]);
#endif

    return (elem & (1u << bitOffset)) != 0u;
#endif
}

#if defined(_LIGHT_COOKIES)
#ifndef REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
#define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR 1
#endif
#endif


float2 ComputeLightCookieUVDirectional(float4x4 worldToLight, float3 samplePositionWS, float4 atlasUVRect, uint2 uvWrap)
{
    // Translate and rotate 'positionWS' into the light space.
    // Project point to light "view" plane, i.e. discard Z.
    float2 positionLS = mul(worldToLight, float4(samplePositionWS, 1)).xy;

    // Remap [-1, 1] to [0, 1]
    // (implies the transform has ortho projection mapping world space box to [-1, 1])
    float2 positionUV = positionLS * 0.5 + 0.5;

    // Tile texture for cookie in repeat mode
    positionUV.x = (uvWrap.x == URP_TEXTURE_WRAP_MODE_REPEAT) ? frac(positionUV.x) : positionUV.x;
    positionUV.y = (uvWrap.y == URP_TEXTURE_WRAP_MODE_REPEAT) ? frac(positionUV.y) : positionUV.y;
    positionUV.x = (uvWrap.x == URP_TEXTURE_WRAP_MODE_CLAMP) ? saturate(positionUV.x) : positionUV.x;
    positionUV.y = (uvWrap.y == URP_TEXTURE_WRAP_MODE_CLAMP) ? saturate(positionUV.y) : positionUV.y;

    // Remap to atlas texture
    float2 positionAtlasUV = atlasUVRect.xy * float2(positionUV)+atlasUVRect.zw;

    return positionAtlasUV;
}

float2 ComputeLightCookieUVSpot(float4x4 worldToLightPerspective, float3 samplePositionWS, float4 atlasUVRect)
{
    // Translate, rotate and project 'positionWS' into the light clip space.
    float4 positionCS = mul(worldToLightPerspective, float4(samplePositionWS, 1));
    float2 positionNDC = positionCS.xy / positionCS.w;

    // Remap NDC to the texture coordinates, from NDC [-1, 1]^2 to [0, 1]^2.
    float2 positionUV = saturate(positionNDC * 0.5 + 0.5);

    // Remap into rect in the atlas texture
    float2 positionAtlasUV = atlasUVRect.xy * float2(positionUV)+atlasUVRect.zw;

    return positionAtlasUV;
}


// Ref: http://jcgt.org/published/0003/02/01/paper.pdf "A Survey of Efficient Representations for Independent Unit Vectors"
// Encode with Oct, this function work with any size of output
// return float between [-1, 1]
float2 PackNormalOctQuadEncode(float3 n)
{
    //float l1norm    = dot(abs(n), 1.0);
    //float2 res0     = n.xy * (1.0 / l1norm);

    //float2 val      = 1.0 - abs(res0.yx);
    //return (n.zz < float2(0.0, 0.0) ? (res0 >= 0.0 ? val : -val) : res0);

    // Optimized version of above code:
    n *= rcp(max(dot(abs(n), 1.0), 1e-6));
    float t = saturate(-n.z);
    return n.xy + float2(n.x >= 0.0 ? t : -t, n.y >= 0.0 ? t : -t);
}

float2 ComputeLightCookieUVPoint(float4x4 worldToLight, float3 samplePositionWS, float4 atlasUVRect)
{
    // Translate and rotate 'positionWS' into the light space.
    float4 positionLS = mul(worldToLight, float4(samplePositionWS, 1));

    float3 sampleDirLS = normalize(positionLS.xyz / positionLS.w);

    // Project direction to Octahederal quad UV.
    float2 positionUV = saturate(PackNormalOctQuadEncode(sampleDirLS) * 0.5 + 0.5);

    // Remap to atlas texture
    float2 positionAtlasUV = atlasUVRect.xy * float2(positionUV)+atlasUVRect.zw;

    return positionAtlasUV;
}



float3 SampleMainLightCookie(float3 samplePositionWS)
{
    if (!IsMainLightCookieEnabled())
        return float3(1, 1, 1);

    float2 uv = ComputeLightCookieUVDirectional(_MainLightWorldToLight, samplePositionWS, float4(1, 1, 0, 0), URP_TEXTURE_WRAP_MODE_NONE);
    float4 color = SampleMainLightCookieTexture(uv);

    return IsMainLightCookieTextureRGBFormat() ? color.rgb
        : IsMainLightCookieTextureAlphaFormat() ? color.aaa
        : color.rrr;
}

float3 SampleAdditionalLightCookie(int perObjectLightIndex, float3 samplePositionWS)
{
    if (!IsLightCookieEnabled(perObjectLightIndex))
        return float3(1, 1, 1);

    int lightType = GetLightCookieLightType(perObjectLightIndex);
    int isSpot = lightType == URP_LIGHT_TYPE_SPOT;
    int isDirectional = lightType == URP_LIGHT_TYPE_DIRECTIONAL;

    float4x4 worldToLight = GetLightCookieWorldToLightMatrix(perObjectLightIndex);
    float4 uvRect = GetLightCookieAtlasUVRect(perObjectLightIndex);

    float2 uv;
    if (isSpot)
    {
        uv = ComputeLightCookieUVSpot(worldToLight, samplePositionWS, uvRect);
    }
    else if (isDirectional)
    {
        uv = ComputeLightCookieUVDirectional(worldToLight, samplePositionWS, uvRect, URP_TEXTURE_WRAP_MODE_REPEAT);
    }
    else
    {
        uv = ComputeLightCookieUVPoint(worldToLight, samplePositionWS, uvRect);
    }

    float4 color = SampleAdditionalLightsCookieAtlasTexture(uv);

    return IsAdditionalLightsCookieAtlasTextureRGBFormat() ? color.rgb
        : IsAdditionalLightsCookieAtlasTextureAlphaFormat() ? color.aaa
        : color.rrr;
}

//EDN COOKIE FIX



Light GetAdditionalLight(uint i, float3 positionWS, half4 shadowMask)
{
#if USE_FORWARD_PLUS
    int lightIndex = i;
#else
    int lightIndex = GetPerObjectLightIndex(i);
#endif
    Light light = GetAdditionalPerObjectLight(lightIndex, positionWS);

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    half4 occlusionProbeChannels = _AdditionalLightsBuffer[lightIndex].occlusionProbeChannels;
#else
    half4 occlusionProbeChannels = _AdditionalLightsOcclusionProbes[lightIndex];
#endif
    light.shadowAttenuation = AdditionalLightShadow(lightIndex, positionWS, light.direction, shadowMask, occlusionProbeChannels);
#if defined(_LIGHT_COOKIES)
    float3 cookieColor = SampleAdditionalLightCookie(lightIndex, positionWS);
    light.color *= cookieColor;
#endif

    return light;
}

float4 GetScaledScreenParams()
{
    return _ScaledScreenParams;
}
void TransformScreenUV(inout float2 uv, float screenHeight)
{
#if UNITY_UV_STARTS_AT_TOP
    uv.y = screenHeight - (uv.y * _ScaleBiasRt.x + _ScaleBiasRt.y * screenHeight);
#endif
}
void TransformNormalizedScreenUV(inout float2 uv)
{
#if UNITY_UV_STARTS_AT_TOP
    TransformScreenUV(uv, 1.0);
#endif
}
float2 GetNormalizedScreenSpaceUV(float2 positionCS)
{
    float2 normalizedScreenSpaceUV = positionCS.xy * rcp(GetScaledScreenParams().xy);
    TransformNormalizedScreenUV(normalizedScreenSpaceUV);
    return normalizedScreenSpaceUV;
}
void AdditionalLightsA_float(float3 SpecColor, float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView, half4 Shadowmask,
    out float3 Diffuse, out float shadowAtten){//, out float3 Specular) {

        float3 diffuseColor = 0;
        float shadowAttenLocals = 0;
        //float3 specularColor = 0;
        #ifndef SHADERGRAPH_PREVIEW
            Smoothness = exp2(10 * Smoothness + 1);
            uint pixelLightCount = GetAdditionalLightsCount();
            //uint meshRenderingLayers = GetMeshRenderingLayer();

        #if USE_FORWARD_PLUS

            // For Foward+ the LIGHT_LOOP_BEGIN macro will use inputData.normalizedScreenSpaceUV, inputData.positionWS, so create that:
            InputData inputData = (InputData)0;
            //float4 screenPos = ComputeScreenPos(TransformWorldToHClip(WorldPosition));
            //inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;

            //Fix lights disappearing 
            float4 screenPos = float4(TransformWorldToHClip(WorldPosition).x, (_ScaledScreenParams.y - TransformWorldToHClip(WorldPosition).y), 0, 0);
            inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(screenPos);

            inputData.positionWS = WorldPosition;

           // uint lightsCount = GetAdditionalLightsCount();// -1;

            for (uint lightIndex = 0; lightIndex < min(9, MAX_VISIBLE_LIGHTS); lightIndex++) {
                //FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
          // uint lightIndex; 
              //  ClusterIterator _urp_internal_clusterIterator = ClusterInit(inputData.normalizedScreenSpaceUV, inputData.positionWS, 0);
              //  [loop] while (ClusterNext(_urp_internal_clusterIterator, lightIndex)) {
                
                //    lightIndex += URP_FP_DIRECTIONAL_LIGHTS_COUNT; 
                    FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK


                    Light light = GetAdditionalLight(lightIndex, inputData.positionWS, Shadowmask);
     /*   #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        #endif*/
                {
                    // Blinn-Phong
                    float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                    diffuseColor += attenuatedLightColor;// LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
                    //specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, float4(SpecColor, 0), Smoothness);
                    shadowAttenLocals += light.distanceAttenuation * light.shadowAttenuation * 1;

                    //diffuseColor += 10;
                }
            }

#else
            // // For Foward+ the LIGHT_LOOP_BEGIN macro will use inputData.normalizedScreenSpaceUV, inputData.positionWS, so create that:
            //InputData inputData = (InputData)0;
            //float4 screenPos = ComputeScreenPos(TransformWorldToHClip(WorldPosition));
            //inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;
            //inputData.positionWS = WorldPosition;

            LIGHT_LOOP_BEGIN(pixelLightCount)
                Light light = GetAdditionalLight(lightIndex, WorldPosition, Shadowmask);
            /*  #ifdef _LIGHT_LAYERS
                  if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
              #endif*/
            {
                // Blinn-Phong
                float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                diffuseColor += attenuatedLightColor;// LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
                //specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, float4(SpecColor, 0), Smoothness);
                shadowAttenLocals += light.distanceAttenuation * light.shadowAttenuation * 1;

            }
            LIGHT_LOOP_END
            
        #endif

           
        #endif

        Diffuse = diffuseColor;
        shadowAtten = shadowAttenLocals;
        //Specular = specularColor;
}
////////////// NEW
//void AdditionalLights_float(float3 SpecColor, float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView, half4 Shadowmask,
   // float PointLightBands, float SpotLightBands,
  //  out float3 Diffuse, out float shadowAtten) {
void AdditionalLights_float(float3 SpecColor, float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView, half4 Shadowmask,
    out float3 Diffuse, out float shadowAtten) {
    //, out float3 Specular) {
    float3 diffuseColor = 0;
    //float3 specularColor = 0;
#ifndef SHADERGRAPH_PREVIEW
    Smoothness = exp2(10 * Smoothness + 1);
    uint pixelLightCount = GetAdditionalLightsCount();
    //uint meshRenderingLayers = GetMeshRenderingLayer();

   // #if USE_FORWARD_PLUS
   // 	for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++) {
   // 		FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
   // 			Light light = GetAdditionalLight(lightIndex, WorldPosition, Shadowmask);
   ///* #ifdef _LIGHT_LAYERS
   // 		if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
   // #endif*/
   // 		{
   // 			// Blinn-Phong
   // 			float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
   //             diffuseColor += attenuatedLightColor;// LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
   // 			//specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, float4(SpecColor, 0), Smoothness);
   // 		}
   // 	}
   // #endif

    //uint pixelLightCountA = int(min(_AdditionalLightsCount.x, unity_LightData.y));

    if (_AdditionalLightsCount.x == 0){// || 1==1) {
        shadowAtten = 0;
        Diffuse = 0;
        return;
    }

    //if (pixelLightCountA > 0) {

        // For Foward+ the LIGHT_LOOP_BEGIN macro will use inputData.normalizedScreenSpaceUV, inputData.positionWS, so create that:
        InputData inputData = (InputData)0;
        float4 screenPos = ComputeScreenPos(TransformWorldToHClip(WorldPosition));
        inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;
        inputData.positionWS = WorldPosition;

        int countme = 0;
        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLight(lightIndex, WorldPosition, Shadowmask);
        //#ifdef _LIGHT_LAYERS
        //	if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        //#endif
        {
            // Blinn-Phong
            float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
            diffuseColor += attenuatedLightColor;// LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
            //specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, float4(SpecColor, 0), Smoothness);
            //diffuseColor += 10;
            countme++;
            if (countme > 9) {
                break;
            }
        }
        LIGHT_LOOP_END
    //}
#endif

        Diffuse = diffuseColor;
    //Specular = specularColor;
    shadowAtten = 0;
}







           // #pragma exclude_renderers gles xbox360 ps3 flash 
            #pragma target 3.0
            uniform float4 _TimeEditor;
            #ifndef LIGHTMAP_OFF
                // float4 unity_LightmapST;
                // sampler2D unity_Lightmap;
                #ifndef DIRLIGHTMAP_OFF
                    // sampler2D unity_LightmapInd;
                #endif
            #endif
            uniform sampler2D _Diffuse; uniform float4 _Diffuse_ST;
            uniform sampler2D _Normal; uniform float4 _Normal_ST;
            

            //v2.2
            float useNoisePattern;
            float4 grassTypeHeights;
            //v2.2a     
            sampler2D _windTexture;
            float4 _windTexture_ST;
            uniform float4 _noiseWindParams;
            //v2.2b
            float _ViewBendStrength = 5;

            //MASK
            uniform sampler2D _maskTexture;
            uniform float4 _maskTexture_ST;
            float patternAPower;
            float4 PatternATilingOffset;
            float patternGroundPower;
            float patternGroundHeight;
            float _shadowBrightFactor;

			//v2.0.8
			sampler2D _InteractTexture;
			float3 _InteractTexturePos;
			//v2.1.1
			float _shapeOnlyHeight;
			//v2.1.12
			float _XTiles;
			float _YTiles;
			
			//v2.1.13
			float4 scaleYFactor;
			//v2.1.14
			float3 _InteractAmpFreqRad;
			float3 _InteractPos2;

            uniform float _BulgeScale; 
            uniform float _BulgeShape;
            uniform float _BulgeScale_copy;
            float4 _WaveControl1;
   			float4 _TimeControl1;
    		float3 _OceanCenter;
            uniform fixed _Cutoff;
             uniform float _RandYScale;
            uniform float _RippleScale;
            //float3 _CameraPos;
            float3 _InteractPos;
            float _FadeThreshold;
            float _StopMotionThreshold;
            
            float3 _Color;
            float3 _ColorGlobal;
           	float _TintPower; 
           	float _SpecularPower;
           	float _SmoothMotionFactor;
           	float _WaveXFactor;
           	float _WaveYFactor;
           	
           	sampler2D _SnowTexture;
           	float _SnowCoverage; 	float _SnowOffset;
           	float _TintFrequency;

           	 float3 _InteractSpeed;
           	 float _InteractMaxYoffset;

			 //v1.9.9.2
			 float shadowBrightnessCutoff;// = 0.1;
			 float shadowBrightness;// = 1;
            
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float4 vertexColor : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID //v1.7.8
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float3 tangentDir : TEXCOORD3;
                float3 binormalDir : TEXCOORD4;
                float4 vertexColor : COLOR;
                LIGHTING_COORDS(5,6)
                #ifndef LIGHTMAP_OFF
                    float2 uvLM : TEXCOORD7;
                #endif
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v); //v1.7.8
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.normalDir = mul(float4(v.normal,0), unity_WorldToObject).xyz;
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.binormalDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                float4 node_389 = o.vertexColor;
                float4 node_392 = _Time + _TimeEditor;
           //     v.vertex.xyz += (normalize((float3(1,0.5,0.5)+v.normal))*node_389.r*sin(((node_389.b*3.141592654)+node_392.g+node_392.b))*0.16);
                

				

					//v2.0.8
				half2 tileableUv = mul(unity_ObjectToWorld, (v.vertex)).xz;
				float WorldScale = _InteractTexturePos.y;
				float3 CamPos = float3(0, 0, 0);//_DepthCameraPos;//_WorldSpaceCameraPos;
				float3 Origin = float3(_InteractTexturePos.x,  _InteractTexturePos.z, 0);//float2(CamPos.x - WorldScale/2.0 , CamPos.z - WorldScale/2.0);
				float2 UnscaledTexPoint = float2(tileableUv.x - Origin.x, tileableUv.y - Origin.y);
				float2 ScaledTexPoint = float2(UnscaledTexPoint.x / WorldScale, UnscaledTexPoint.y / WorldScale);
				float4 texInteract = tex2Dlod(_InteractTexture, float4(ScaledTexPoint, 0.0, 0.0));



                
                float dist = distance(_OceanCenter, float3(_WaveControl1.x*mul(unity_ObjectToWorld, v.vertex).y,_WaveControl1.y*mul(unity_ObjectToWorld, v.vertex).x,_WaveControl1.z*mul(unity_ObjectToWorld, v.vertex).z) );
                float dist2 = distance(_OceanCenter, float3(mul(unity_ObjectToWorld, v.vertex).y,mul(unity_ObjectToWorld, v.vertex).x*0.10,0.1*mul(unity_ObjectToWorld, v.vertex).z) );
                
                float node_5027 = (_Time.y*_TimeControl1.x + _TimeEditor);//*sin(dist + 1.5*dist*pi);
                float node_133 = pow((abs((frac((o.uv0+node_5027*float2(0.2,0.1)).r)-0.5))*2.0),_BulgeShape);
                            
                       
                       //INIFNIGRASS
                       float4 modelY = float4(0.0,1.0,0.0,0.0);
                               float4 ModelYWorld =mul(unity_ObjectToWorld,modelY);
                               float scaleY = length(ModelYWorld);
                                
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
              // o.posWorld =  v.vertex;


                  //v2.2
                if (useNoisePattern != 0) {
                    //brushID = 255 * texInteract.r * useNoisePattern;
                    texInteract.a = texInteract.r * useNoisePattern;
                    texInteract.r = 0;
                    texInteract.g = 0;
                    texInteract.b = 0;
                }


				//v.vertex = o.posWorld;
				//v2.0.8
				if (_shapeOnlyHeight == 0) { //v2.1.1
					o.posWorld.y = o.posWorld.y*clamp((1 - texInteract.r*texInteract.r + 0.001)*1.5, 0, 1);
					o.posWorld.y = o.posWorld.y - texInteract.r * 2; //v2.1.14
					if (o.uv0.y > 0.01) {
						//o.posWorld.y = o.posWorld.y - texInteract.b * 2;

						if (texInteract.g >= 0.75) {
							o.posWorld.x = o.posWorld.x - (1 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
							o.posWorld.z = o.posWorld.z - (texInteract.g - 0.75)*o.uv0.y * 42 * texInteract.b;
						}
						else if (texInteract.g >= 0.5) {	//negative angle region					
							o.posWorld.x = o.posWorld.x - (texInteract.g - 0.5)*o.uv0.y * 42 * texInteract.b;
							o.posWorld.z = o.posWorld.z + (0.75 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
						}
						else if (texInteract.g >= 0.25) {
							o.posWorld.x = o.posWorld.x + (0.5 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
							o.posWorld.z = o.posWorld.z + (0.25 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
						}
						else if (texInteract.g > 0) {
							o.posWorld.x = o.posWorld.x + texInteract.g*o.uv0.y * 42 * texInteract.b;
							o.posWorld.z = o.posWorld.z + (0.25 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
						}
					}

					//v2.1.16 - _grassNormal
					//v.vertex.y = v.vertex.y*_respectHeight+ (pow(255-tex.r*255,_ShoreFadeFactor) - 25*pow(1+1-tex.r,1.5) + 8  + o.uv0.y*_grassHeight ); 
					//if (_textureFeed == 1) { //v2.1.20b
					//	v.vertex.y = v.vertex.y*_respectHeight + (pow(942.64 - tex.r*942.64, _ShoreFadeFactor) - 0 + o.uv0.y*_grassHeight);
					///}
					//					        if(v.vertex.y > 25*_grassHeight){
					//					       		v.vertex.y =v.vertex.y*_respectHeight + o.uv0.y*_grassHeight + 8;
					//					        }

				}
				else {
					//v.vertex.y = v.vertex.y-texInteract.b*_shapeOnlyHeight*2;
					//v.vertex.y = abs(v.vertex.y)-pow(0+texInteract.r,_shapeOnlyHeight);
					//v.vertex.y = v.vertex.y*((texInteract.r+0.011)*12.5);
					o.posWorld.y = abs(o.posWorld.y) - sign(o.posWorld.y)*(1 - texInteract.r)*_shapeOnlyHeight;

					//if (_textureFeed == 1) { //v2.1.20b
					//	v.vertex.y = v.vertex.y*_respectHeight + (pow(255 - tex.r * 255, _ShoreFadeFactor) - 25 * pow(1 + 1 - tex.r, 1.5) + 8);
					//	v.vertex.y = abs(v.vertex.y)*_grassHeight;
					//}
				}
				//o.posWorld = v.vertex;


                                                                                                  
//                if( distance(_InteractPos,o.posWorld) < _StopMotionThreshold){                 
//                	_BulgeScale = 0;
//                	_BulgeScale_copy = 0;
//                }
//
  float3 SpeedFac = float3(0,0,0);  //	SpeedFac =  _InteractSpeed;  
                float distA =  distance(_InteractPos,o.posWorld)/ (_StopMotionThreshold*1);      
                  if( distance(_InteractPos,o.posWorld) < _StopMotionThreshold*1){ 
               // if( distance(_InteractPos.x,o.posWorld.x) < _StopMotionThreshold/2 ){    
               //      if( distance(_InteractPos.z,o.posWorld.z) < _StopMotionThreshold/2){                  
                	//_BulgeScale = 0;
                	//_BulgeScale_copy = 0;                	
                	SpeedFac =  _InteractSpeed *_WaveControl1.w;
                		if( o.uv0.y > 0.2){
							o.posWorld.x += (_InteractSpeed.x*1+0.1)*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/5) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/5);
							o.posWorld.z += (_InteractSpeed.z+0.1)*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/5) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/6);
						}
                	if( o.uv0.y < 0.3){
                	//_WaveXFactor = _WaveXFactor - (1-distA)*(1-distA)*SpeedFac.z;
                	//_WaveYFactor = _WaveYFactor - (1-distA)*(1-distA)*SpeedFac.x;
                	}
                	if( o.uv0.y > 0.19){
						_WaveXFactor = _WaveXFactor - (1-distA)*(1-distA)*SpeedFac.z*1;
                		_WaveYFactor = _WaveYFactor - (1-distA)*(1-distA)*SpeedFac.x*1;
                	}
                	if( o.uv0.y > 0.5){
						//o.posWorld.y = o.posWorld.y - (1-distA)*3*(o.uv0.y-0.5)*sin(o.posWorld.z+_Time.y) ;//+_BulgeScale*0.5*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) ;
						o.posWorld.y = o.posWorld.y - (1-distA)*_InteractMaxYoffset*(o.uv0.y-0.5)*sin(o.posWorld.z+_Time.y) ; //v2.0.6
					}             
               }

				  //2.1.14
				  /////////////////////////////////////////////////////////////////////////////////////////// LOCAL INTERACTOR 2 /////////////////////////////////////////////
				  SpeedFac = float3(0, 0, 0);
				  distA = distance(_InteractPos2, o.posWorld) / (_InteractAmpFreqRad.z * 1);
				  if (distance(_InteractPos2, o.posWorld) < _InteractAmpFreqRad.z * 1) {
					  SpeedFac = 3 * (o.posWorld - _InteractPos2) *_WaveControl1.w 
						  + _InteractAmpFreqRad.z*(1.1*cross(float3(0, 1, 0.5), o.posWorld - _InteractPos2) - 2.71*cross(float3(0, 1, 0), _InteractPos2 - o.posWorld)) 
						  + _InteractAmpFreqRad.x*(o.posWorld - _InteractPos2) *_WaveControl1.w *sin(o.posWorld.z + _InteractAmpFreqRad.y*_Time.y);
					  _BulgeScale = _BulgeScale * (1 / (distA + 0.01));

					  if (o.uv0.y < 0.3) {

					  }
					  if (o.uv0.y > 0.19) {
						  _WaveXFactor = _WaveXFactor - (1 - distA)*(1 - distA)*SpeedFac.z;
						  _WaveYFactor = _WaveYFactor - (1 - distA)*(1 - distA)*SpeedFac.x;
					  }
					  if (o.uv0.y > 0.5) {
						  o.posWorld.y = o.posWorld.y - (1 - distA) * 3 * (o.uv0.y - 0.5)*sin(o.posWorld.z + _Time.y);
					  }
				  }
				  /////////////////////////////////////////////////////////////////////////////////////////// END LOCAL INTERACTOR 2 /////////////////////////////////////////////


	                if( o.uv0.y > 0.1){
	       //         	v.vertex.xyz += (node_133*(_BulgeScale*sin(_TimeControl1.w*_Time.y +_TimeControl1.z + dist) )*v.normal*(v.normal*_BulgeScale_copy)) /scaleY;// unity_Scale.w;
					}
	                if( o.uv0.y >= 0.01){
	       //         	v.vertex.y = v.vertex.y *_RandYScale* abs(cos(((_TimeControl1.w*_Time.y +_TimeControl1.z)*0.2 + 2*dist)*_RippleScale));
	                }

	                //v1.5
	           _BulgeScale= _BulgeScale* _BulgeScale_copy;
	            //  _OceanCenter.x = 0.0;
	          // _OceanCenter.z = 0.0;

	                     dist = 90* (cos(_BulgeShape+_Time.y/15))-_SmoothMotionFactor;
				///////////////////////// 
				if( o.uv0.y > 0.1){
					o.posWorld.x += _BulgeScale*1*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/5) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/5);
					o.posWorld.z += _BulgeScale*1*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/5) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/6);
				}
				if( o.uv0.y > 0.2){					
					o.posWorld.x += _BulgeScale*2*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/3) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/3);
					o.posWorld.z += _BulgeScale*2*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/3) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/3);	
				}
				if( o.uv0.y > 0.3){
					
					o.posWorld.x += _BulgeScale*3*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/3) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/4);
					o.posWorld.z += _BulgeScale*3*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/3) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/3);
				}
				if( o.uv0.y > 0.4){
					
					o.posWorld.x += _BulgeScale*4*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/2) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/2);
					o.posWorld.z += _BulgeScale*4*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/2) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/2);	
				}		
				if( o.uv0.y > 0.96){
					
					o.posWorld.x += _BulgeScale*5*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/0.9)	+ _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/1);
					o.posWorld.z += _BulgeScale*5*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/0.9) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/1);
				}	


				//v2.1.13
				if (o.uv0.y > 0.16) {
					o.posWorld.y = o.posWorld.y * scaleYFactor.x + scaleYFactor.y;
				}


                //v2.2a     
                float2 windFromNoiseTex = tex2Dlod(_windTexture, 
                    float4(o.posWorld.xz * _windTexture_ST.xy + (_noiseWindParams.xy * _noiseWindParams.w * _Time.y * 0.0001), 0.0, 0.0)).xy;
                float2 windTX = _noiseWindParams.z * (2.0 * windFromNoiseTex - 1);
                float noiseWindPow = length(windFromNoiseTex);
                if (o.uv0.y > 0.14) {
                    o.posWorld.xz += windTX;
                }
                //o.posWorld.w = noiseWindPow;//v2.2a  
				
                                                                                




				 //v2.1.12 - 2.0.8
                if (_XTiles > 1 || _YTiles > 1) {
                    float tilesX = _XTiles;
                    float tilesY = _YTiles;
                    float brushCount = tilesX * tilesY; //scale factor
                    o.uv0.x = o.uv0.x / tilesX;
                    o.uv0.y = o.uv0.y / tilesY;
                    int brushID = 255 * texInteract.a;//clamp(255-texInteract.r*255,0,255)/brushCount;//e.g. 5
                    int division = (brushID + 1) / tilesX;//e.g. 2
                    float ypoloipo = (brushID + 1) - (division * tilesX);//e.g. 5 - 2*2 = 5-4 = 1
                    int sub = -1;
                    if (ypoloipo > 0) {
                        sub = 0;
                    }
                  /*  o.uv0.x = o.uv0.x + (ypoloipo - 1) * (1 / tilesX);
                    o.uv0.y = o.uv0.y + (division + sub) * (1 / tilesY);*/

                    //v2.2
                    if (useNoisePattern != 0) {
                        if (texInteract.a >= 0.75) {
                            o.uv0.x = o.uv0.x + 0.5;
                            o.uv0.y = o.uv0.y + 0.5;
                            brushID = 0;
                        }
                        else if (texInteract.a >= 0.5) {
                            o.uv0.x = o.uv0.x + 0.0;
                            o.uv0.y = o.uv0.y + 0.5;
                            brushID = 1;
                        }
                        else if (texInteract.a >= 0.25) {
                            o.uv0.x = o.uv0.x + 0.5;
                            o.uv0.y = o.uv0.y + 0.0;
                            brushID = 2;
                        }
                        else if (texInteract.a >= 0) {
                            o.uv0.x = o.uv0.x + 0.0;
                            o.uv0.y = o.uv0.y + 0.0;
                            brushID = 3;
                        }

                        //int brushID = 255 * texInteract.a;//clamp(255-texInteract.r*255,0,255)/brushCount;//e.g. 5
                        //division = (brushID + 1) / tilesX;//e.g. 2
                        //ypoloipo = (brushID + 1) - (division * tilesX);//e.g. 5 - 2*2 = 5-4 = 1
                        //sub = -1;
                        //if (ypoloipo > 0) {
                        //    sub = 0;
                        //}

                        float dispX = (ypoloipo - 1) * (1 / tilesX);
                        float dispY = (division + sub) * (1 / tilesY);
                        dispX = clamp(dispX, 0, 1);
                        dispY = clamp(dispY, 0, 1);
                        int timesX = (int)(dispX / 0.5);
                        int timesY = (int)(dispY / 0.5);
                        //o.uv0.x = o.uv0.x + timesX * 0.5;
                        //o.uv0.y = o.uv0.y + timesY * 0.5;
                        if (dispX != 0 || dispX != 0.5 || dispX != 1) {
                            dispX = 0;
                        }
                        if (dispY != 0 || dispY != 0.5 || dispY != 1) {
                            dispY = 0;
                        }
                        o.uv0.x = o.uv0.x + dispX;
                        o.uv0.y = o.uv0.y + dispY;

                        if (o.uv0.y > 0.16) {
                            //grassTypeHeights
                            o.posWorld.y = o.posWorld.y //grassTypeHeights.x * max(dispX,1) * max(dispY, 1);
                                + grassTypeHeights.x * max(dispX + dispY, 1)* (1-texInteract.a) * (1 - 0.2*pow(noiseWindPow, 1)) + grassTypeHeights.y * max(dispX + dispY, 1)
                                + grassTypeHeights.z * max(dispX + dispY, 1) + grassTypeHeights.w * max(dispX + dispY, 1);
                        }
                    }
                    else {
                        o.uv0.x = o.uv0.x + (ypoloipo - 1) * (1 / tilesX);
                        o.uv0.y = o.uv0.y + (division + sub) * (1 / tilesY);
                    }
				}


                //v2.2b
                float viewDot = dot(v.normal, UNITY_MATRIX_IT_MV[2].xyz);
                if (o.uv0.y > 0.05) {
                    o.posWorld.xz += UNITY_MATRIX_IT_MV[1].xz * _ViewBendStrength * saturate(viewDot);
                }

                //ADD GLOBAL ROTATION - WIND						
                v.vertex = mul(unity_WorldToObject, o.posWorld);
                // v.vertex =  o.posWorld;


                o.posWorld.w = noiseWindPow;//v2.2a  

                
                o.pos = UnityObjectToClipPos(v.vertex);
                #ifndef LIGHTMAP_OFF
                    o.uvLM = v.texcoord1 * unity_LightmapST.xy + unity_LightmapST.zw;
                #endif
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            
            
            float4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.binormalDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
/////// Normals:
                float2 node_582 = i.uv0;
                float3 normalLocal = UnpackNormal(tex2D(_Normal,TRANSFORM_TEX(node_582.rg, _Normal))).rgb;
                float3 normalDirection =  normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                
                float nSign = sign( dot( viewDirection, i.normalDir ) ); // Reverse normal if this is a backface
                i.normalDir *= nSign;
                normalDirection *= nSign;
                
                float4 node_1 = tex2D(_Diffuse,TRANSFORM_TEX(node_582.rg, _Diffuse));

                //v2.2a  
                node_1 = (node_1 + 2*node_1*pow(1-i.posWorld.w,3))*1.25;
                
                
                clip(node_1.a - _Cutoff);
                
                //DEFINE FADE BASED ON CAMERA - INFINIGRASS
				float Aplha = 1;
				if(distance(i.posWorld, _WorldSpaceCameraPos) > _FadeThreshold){
					 clip(-1);
				}
                
                #ifndef LIGHTMAP_OFF
                    float4 lmtex = UNITY_SAMPLE_TEX2D(unity_Lightmap,i.uvLM);
                    #ifndef DIRLIGHTMAP_OFF
                        float3 lightmap = DecodeLightmap(lmtex);
                        float3 scalePerBasisVector = DecodeLightmap(UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd,unity_Lightmap,i.uvLM));
                        UNITY_DIRBASIS
                        half3 normalInRnmBasis = saturate (mul (unity_DirBasis, normalLocal));
                        lightmap *= dot (normalInRnmBasis, scalePerBasisVector);
                    #else
                        float3 lightmap = DecodeLightmap(lmtex);
                    #endif
                #endif
                #ifndef LIGHTMAP_OFF
                    #ifdef DIRLIGHTMAP_OFF
                        float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                    #else
                        float3 lightDirection = normalize (scalePerBasisVector.x * unity_DirBasis[0] + scalePerBasisVector.y * unity_DirBasis[1] + scalePerBasisVector.z * unity_DirBasis[2]);
                        lightDirection = mul(lightDirection,tangentTransform); // Tangent to world
                    #endif
                #else
                    float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                #endif
                
                
                lightDirection =  normalize(reflect(_WorldSpaceLightPos0.xyz,normalDirection));
                
                
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                //float attenuation = LIGHT_ATTENUATION(i); 
               // UNITY_LIGHT_ATTENUATION(attenuation, i, i.posWorld.xyz);
                
                //v0.1
                float attenuation = 1;
                //v0.1
                half cascadeIndex = ComputeCascadeIndex(i.posWorld);
                half4 shadowCoord = mul(_MainLightWorldToShadow[cascadeIndex], float4(i.posWorld.xyz, 1.0));
                //half distAtten  unity_LightData.z;
                //half shadowAtten = MainLightRealtimeShadow(shadowCoord);
                attenuation = MainLightRealtimeShadow(shadowCoord);
                //v0.1b
                float shadowAttenLocals = 0;
                float3 diffuseLocalLights = float3(0, 0, 0);
                float4 Shadowmask = float4(1, 1, 1, 1);
                //Shadowmask_half(float2 lightmapUV, out half4 Shadowmask){
                AdditionalLights_float(float3(0, 0, 0), float3(0, 0, 0), i.posWorld.xyz, normalDirection, float3(0, 0, 0), Shadowmask,
                     diffuseLocalLights, shadowAttenLocals);
                //attenuation = shadowAttenLocals*1;

                   //INFINIGRASS
                 float node_5027 = (_Time.y*_TimeControl1.x + _TimeEditor);
                 float node_133 = pow((abs((frac((i.uv0+node_5027*float2(0.2,0.1)).r)-0.5))*2.0),_BulgeShape);
                   float dist = distance(_OceanCenter, float3(_WaveControl1.x*i.posWorld.y,_WaveControl1.y*i.posWorld.x,_WaveControl1.z*i.posWorld.z) );
               
               float3 attenColor = attenuation * (_LightColor0.xyz);
               
               //v0.1b
               attenColor += diffuseLocalLights;
               
/////// Diffuse:
                float NdotL = dot( normalDirection, lightDirection );
                float3 w = float3(0.9,0.9,0.8)*0.5; // Light wrapping
                float3 NdotLWrap = NdotL * ( 1.0 - w );
                float3 forwardLight = max(float3(0.0,0.0,0.0), NdotLWrap + w );
                float3 backLight = max(float3(0.0,0.0,0.0), -NdotLWrap + w ) ;//* float3(0.9,1,0.5); //v1.4
                #ifndef LIGHTMAP_OFF
                    float3 diffuse = lightmap.rgb;
                #else
                    float3 diffuse = (forwardLight+backLight) * attenColor + half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);// UNITY_LIGHTMODEL_AMBIENT.rgb;
                #endif
///////// Gloss:
                float gloss = 0.4;
                float specPow = exp2( gloss * 10.0+1.0);
////// Specular:
                NdotL = max(0.0, NdotL);
                float node_3 = 0.2;
                float3 specularColor = float3(node_3,node_3,node_3);
                float3 specular = 3 * pow(max(0,dot(halfDirection,normalDirection)),specPow) * specularColor;
//                #ifndef LIGHTMAP_OFF
//                    #ifndef DIRLIGHTMAP_OFF
//                        specular *= lightmap;
//                    #else
//                        specular *= (floor(attenuation) * _LightColor0.xyz);
//                    #endif
//                #else
//                    specular *= (floor(attenuation) * _LightColor0.xyz);
//                #endif
                specular *= ((attenuation) * _LightColor0.xyz);
                float3 finalColor = 0;
                float3 diffuseLight = diffuse;
                float node_331 = 1.0;
             //   finalColor += diffuseLight * (lerp(float3(node_331,node_331,node_331),float3(0.9632353,0.8224623,0.03541304),i.vertexColor.b)*node_1.rgb);
                finalColor += diffuseLight * (node_1.rgb)*_ColorGlobal; //v1.4
                
                specular = specular * (i.uv0.y*2-0.5) ;
                finalColor = lerp(finalColor, finalColor*_Color,_TintPower*i.uv0.y*(0.9+0.6*cos(i.posWorld.x*2*_TintFrequency)+0.6*sin(i.posWorld.z*3*_TintFrequency)+0.6*sin(i.posWorld.z*1*_TintFrequency+0.1)));
                
                finalColor += specular * _SpecularPower;
/// Final Color:

				//SNOW
                float3 col = finalColor;
                
                float4 SnowTexColor = tex2D(_SnowTexture,  i.uv0);
				
				//if(i.uv0.y >= 1-(3 * (_SnowCoverage+_TimeControl1.y-1)) * col.r* col.r)
				//if(i.uv0.y >= 1-(4 * (_SnowCoverage+_TimeControl1.y-1)) * col.r* col.r* col.r+0.01) //v1.7.6
				if(i.uv0.y >= 1-(4 * (_SnowCoverage + _SnowOffset  +_TimeControl1.y-1)) *clamp(col.r,0.85*(col.r+0.2),1)* clamp(col.r,0.35,1)* 1+0.01) //v2.0.7
	            {     	  
	            	if(i.uv0.y < 0.99 ){    //v1.7.6
		                //col =  lerp (  col , SnowTexColor*0.9,1-(0.5 * _SnowCoverage)) ;   
		                //    col = col * input.color * input.color.a *_UnityTerrainTreeTintColorSM *1.5;
		                //o.Normal = normalize(o.Normal + UnpackNormal(tex2D(_SnowBump, IN.uv_SnowBump))*1);   
		                //col.rgb = float4(i.uv0.y,i.uv0.y,i.uv0.y,1)*4*+_TimeControl1.z; 
		                //v1.7.6
		               // col.rgb = (float4(i.uv0.y,i.uv0.y,i.uv0.y,1)*4*+_TimeControl1.z)*(1+finalColor)*1.6;  
		               col.rgb = (float4(i.uv0.y,i.uv0.y,i.uv0.y,1)*4*+_TimeControl1.z)*(1+finalColor)*clamp(col.r,0.85*(col.r+0.2),1);   //v2.0.7
	                }                      
	            }
	            else
	            {
					//col = col * input.color * input.color.a *_UnityTerrainTreeTintColorSM *1.5;						
				//	col.rgb *= input.color.rgb;
				//	clip(col.a);
				//	col=col* _UnityTerrainTreeTintColorSM;
				}
                
                //END SNOW

				//v1.9.9.2
				//float shadowBrightnessCutoff = 0.1;
				//float shadowBrightness = 1;
				float SBCut = shadowBrightnessCutoff;
				if (col.r < SBCut && col.g < SBCut && col.b < SBCut) {
				//if (col.r < 0.1 && col.g < 0.1 && col.b < 0.1) {
					//col = col * (0.1 - col.r)* (0.1 - col.g)* (0.1 - col.b)*5111;
					float dist = sqrt((SBCut - col.r)* (SBCut - col.r) + (SBCut - col.g)*(SBCut - col.g) + (SBCut - col.b)*(SBCut - col.b));
					col = col + dist * float3(1, 1, 1) * 0.05 * shadowBrightness;
				}

                //MASK - i.posWorld
                float4 maskTextureA = tex2D(_maskTexture, i.posWorld.xz * PatternATilingOffset.xy + PatternATilingOffset.zw);// +i.posWorld.xz * PatternATilingOffset.zw);
                //return float4(maskTextureA.rgb,1);

                float3 maskCol = patternAPower * (maskTextureA.rgb);// *maskTextureA.a);
                col = col + maskCol * (attenuation + (1- attenuation)*_shadowBrightFactor);

                float difff = patternGroundHeight-i.uv0.y;
                if (patternGroundPower > 0 && i.uv0.y< patternGroundHeight) {
                    col = col* maskTextureA* patternGroundPower * difff + col*(1-difff);
                }

                return fixed4(col,1);
            }
            ENDCG
        }





        Pass {
            Name "ForwardAdd"
            Tags {
                "LightMode"="ForwardAdd"
            }
            Blend One One
            Cull Off
            
            
            Fog { Color (0,0,0,0) }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            //#define UNITY_PASS_FORWARDADD
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fwdbase nolightmap //v4.1
           // #pragma exclude_renderers gles xbox360 ps3 flash 
           #pragma multi_compile_instancing //v1.7.8
            #pragma target 3.0
            uniform float4 _TimeEditor;
            #ifndef LIGHTMAP_OFF
                // float4 unity_LightmapST;
                // sampler2D unity_Lightmap;
                #ifndef DIRLIGHTMAP_OFF
                    // sampler2D unity_LightmapInd;
                #endif
            #endif
            uniform sampler2D _Diffuse; uniform float4 _Diffuse_ST;
            uniform sampler2D _Normal; uniform float4 _Normal_ST;
            

            //v2.2
            float useNoisePattern;
            float4 grassTypeHeights;
            //v2.2a     
            sampler2D _windTexture;
            float4 _windTexture_ST;
            uniform float4 _noiseWindParams;
            //v2.2b
            float _ViewBendStrength = 5;

            //v1.8
            float _localLightFactor;

			//v2.0.8
			sampler2D _InteractTexture;
			float3 _InteractTexturePos;
			//v2.1.1
			float _shapeOnlyHeight;
			//v2.1.12
			float _XTiles;
			float _YTiles;

			//v2.1.13
			float4 scaleYFactor;
			//v2.1.14
			float3 _InteractAmpFreqRad;
			float3 _InteractPos2;

               uniform float _BulgeScale; 
            uniform float _BulgeShape;
            uniform float _BulgeScale_copy;
            float4 _WaveControl1;
   			float4 _TimeControl1;
    		float3 _OceanCenter;
            uniform fixed _Cutoff;
             uniform float _RandYScale;
            uniform float _RippleScale;
            
            float3 _InteractPos;
            float _FadeThreshold;
            float _StopMotionThreshold;
            float _SmoothMotionFactor;
            float _WaveXFactor;
           	float _WaveYFactor;


           	//v2.0.9
           	 float3 _Color;
            float3 _ColorGlobal;
           	float _TintPower; 
           	float _SpecularPower;
           
           	
           	sampler2D _SnowTexture;
           	float _SnowCoverage; 	float _SnowOffset;
           	float _TintFrequency;

           	 float3 _InteractSpeed;
           	 float _InteractMaxYoffset;


           	
            
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float4 vertexColor : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID //v1.7.8
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float3 tangentDir : TEXCOORD3;
                float3 binormalDir : TEXCOORD4;
                float4 vertexColor : COLOR;
                LIGHTING_COORDS(5,6)
               // UNITY_SHADOW_COORDS(7)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v); //v1.7.8
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.normalDir = mul(float4(v.normal,0), unity_WorldToObject).xyz;
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.binormalDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                float4 node_389 = o.vertexColor;
                float4 node_392 = _Time + _TimeEditor;



				//v2.0.8
				half2 tileableUv = mul(unity_ObjectToWorld, (v.vertex)).xz;
				float WorldScale = _InteractTexturePos.y;
				float3 CamPos = float3(0, 0, 0);//_DepthCameraPos;//_WorldSpaceCameraPos;
				float3 Origin = float3(_InteractTexturePos.x, _InteractTexturePos.z, 0);//float2(CamPos.x - WorldScale/2.0 , CamPos.z - WorldScale/2.0);
				float2 UnscaledTexPoint = float2(tileableUv.x - Origin.x, tileableUv.y - Origin.y);
				float2 ScaledTexPoint = float2(UnscaledTexPoint.x / WorldScale, UnscaledTexPoint.y / WorldScale);
				float4 texInteract = tex2Dlod(_InteractTexture, float4(ScaledTexPoint, 0.0, 0.0));




            //    v.vertex.xyz += (normalize((float3(1,0.5,0.5)+v.normal))*node_389.r*sin(((node_389.b*3.141592654)+node_392.g+node_392.b))*0.16);
                
                float dist = distance(_OceanCenter, float3(_WaveControl1.x*mul(unity_ObjectToWorld, v.vertex).y,_WaveControl1.y*mul(unity_ObjectToWorld, v.vertex).x,_WaveControl1.z*mul(unity_ObjectToWorld, v.vertex).z) );
                float dist2 = distance(_OceanCenter, float3(mul(unity_ObjectToWorld, v.vertex).y,mul(unity_ObjectToWorld, v.vertex).x*0.10,0.1*mul(unity_ObjectToWorld, v.vertex).z) );
                
                float node_5027 = (_Time.y*_TimeControl1.x + _TimeEditor);//*sin(dist + 1.5*dist*pi);
                float node_133 = pow((abs((frac((o.uv0+node_5027*float2(0.2,0.1)).r)-0.5))*2.0),_BulgeShape);
                               
                                 //INIFNIGRASS
                       float4 modelY = float4(0.0,1.0,0.0,0.0);
                               float4 ModelYWorld =mul(unity_ObjectToWorld,modelY);
                               float scaleY = length(ModelYWorld);     
                               
                  o.posWorld = mul(unity_ObjectToWorld, v.vertex);
             //  o.posWorld =  v.vertex;
                                      
//                if( distance(_InteractPos,o.posWorld) <  _StopMotionThreshold){                 
//                	_BulgeScale = 0;
//                	_BulgeScale_copy = 0;
//                }


                   //v2.2
                  if (useNoisePattern != 0) {
                      //brushID = 255 * texInteract.r * useNoisePattern;
                      texInteract.a = texInteract.r * useNoisePattern;
                      texInteract.r = 0;
                      texInteract.g = 0;
                      texInteract.b = 0;
                  }

				  //v.vertex = o.posWorld;
				//v2.0.8
				  if (_shapeOnlyHeight == 0) { //v2.1.1
					  o.posWorld.y = o.posWorld.y*clamp((1 - texInteract.r*texInteract.r + 0.001)*1.5, 0, 1);
					  o.posWorld.y = o.posWorld.y - texInteract.r * 2; //v2.1.14
					  if (o.uv0.y > 0.01) {
						  //o.posWorld.y = o.posWorld.y - texInteract.b * 2;

						  if (texInteract.g >= 0.75) {
							  o.posWorld.x = o.posWorld.x - (1 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
							  o.posWorld.z = o.posWorld.z - (texInteract.g - 0.75)*o.uv0.y * 42 * texInteract.b;
						  }
						  else if (texInteract.g >= 0.5) {	//negative angle region					
							  o.posWorld.x = o.posWorld.x - (texInteract.g - 0.5)*o.uv0.y * 42 * texInteract.b;
							  o.posWorld.z = o.posWorld.z + (0.75 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
						  }
						  else if (texInteract.g >= 0.25) {
							  o.posWorld.x = o.posWorld.x + (0.5 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
							  o.posWorld.z = o.posWorld.z + (0.25 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
						  }
						  else if (texInteract.g > 0) {
							  o.posWorld.x = o.posWorld.x + texInteract.g*o.uv0.y * 42 * texInteract.b;
							  o.posWorld.z = o.posWorld.z + (0.25 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
						  }
					  }

					  //v2.1.16 - _grassNormal
					  //v.vertex.y = v.vertex.y*_respectHeight+ (pow(255-tex.r*255,_ShoreFadeFactor) - 25*pow(1+1-tex.r,1.5) + 8  + o.uv0.y*_grassHeight ); 
					  //if (_textureFeed == 1) { //v2.1.20b
					  //	v.vertex.y = v.vertex.y*_respectHeight + (pow(942.64 - tex.r*942.64, _ShoreFadeFactor) - 0 + o.uv0.y*_grassHeight);
					  ///}
					  //					        if(v.vertex.y > 25*_grassHeight){
					  //					       		v.vertex.y =v.vertex.y*_respectHeight + o.uv0.y*_grassHeight + 8;
					  //					        }

				  }
				  else {
					  //v.vertex.y = v.vertex.y-texInteract.b*_shapeOnlyHeight*2;
					  //v.vertex.y = abs(v.vertex.y)-pow(0+texInteract.r,_shapeOnlyHeight);
					  //v.vertex.y = v.vertex.y*((texInteract.r+0.011)*12.5);
					  o.posWorld.y = abs(o.posWorld.y) - sign(o.posWorld.y)*(1 - texInteract.r)*_shapeOnlyHeight;

					  //if (_textureFeed == 1) { //v2.1.20b
					  //	v.vertex.y = v.vertex.y*_respectHeight + (pow(255 - tex.r * 255, _ShoreFadeFactor) - 25 * pow(1 + 1 - tex.r, 1.5) + 8);
					  //	v.vertex.y = abs(v.vertex.y)*_grassHeight;
					  //}
				  }
				  //o.posWorld = v.vertex;



float3 SpeedFac = float3(0,0,0);  //	SpeedFac =  _InteractSpeed;  
                float distA =  distance(_InteractPos,o.posWorld)/ (_StopMotionThreshold*1);      
                  if( distance(_InteractPos,o.posWorld) < _StopMotionThreshold*1){ 
               // if( distance(_InteractPos.x,o.posWorld.x) < _StopMotionThreshold/2 ){    
               //      if( distance(_InteractPos.z,o.posWorld.z) < _StopMotionThreshold/2){                  
                	//_BulgeScale = 0;
                	//_BulgeScale_copy = 0;                	
                	SpeedFac =  _InteractSpeed * _WaveControl1.w;
//                		if( o.uv0.y > 0.2){
//							o.posWorld.x += (_InteractSpeed.x*22+0.1)*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/5) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/5);
//							o.posWorld.z += (_InteractSpeed.z+0.1)*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/5) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/6);
//						}
                	if( o.uv0.y < 0.3){
                	//_WaveXFactor = _WaveXFactor - (1-distA)*(1-distA)*SpeedFac.z;
                	//_WaveYFactor = _WaveYFactor - (1-distA)*(1-distA)*SpeedFac.x;
                	}
                	if( o.uv0.y > 0.19){
						_WaveXFactor = _WaveXFactor - (1-distA)*(1-distA)*SpeedFac.z*1;
                		_WaveYFactor = _WaveYFactor - (1-distA)*(1-distA)*SpeedFac.x*1;
                	}
                	if( o.uv0.y > 0.5){
						o.posWorld.y = o.posWorld.y - (1-distA)*_InteractMaxYoffset*(o.uv0.y-0.5)*sin(o.posWorld.z+_Time.y) ;//+_BulgeScale*0.5*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) ;
					}             
               }

				  //2.1.14
				  /////////////////////////////////////////////////////////////////////////////////////////// LOCAL INTERACTOR 2 /////////////////////////////////////////////
				  SpeedFac = float3(0, 0, 0);
				  distA = distance(_InteractPos2, o.posWorld) / (_InteractAmpFreqRad.z * 1);
				  if (distance(_InteractPos2, o.posWorld) < _InteractAmpFreqRad.z * 1) {
					  SpeedFac = 3 * (o.posWorld - _InteractPos2) *_WaveControl1.w
						  + _InteractAmpFreqRad.z*(1.1*cross(float3(0, 1, 0.5), o.posWorld - _InteractPos2) - 2.71*cross(float3(0, 1, 0), _InteractPos2 - o.posWorld))
						  + _InteractAmpFreqRad.x*(o.posWorld - _InteractPos2) *_WaveControl1.w *sin(o.posWorld.z + _InteractAmpFreqRad.y*_Time.y);
					  _BulgeScale = _BulgeScale * (1 / (distA + 0.01));

					  if (o.uv0.y < 0.3) {

					  }
					  if (o.uv0.y > 0.19) {
						  _WaveXFactor = _WaveXFactor - (1 - distA)*(1 - distA)*SpeedFac.z;
						  _WaveYFactor = _WaveYFactor - (1 - distA)*(1 - distA)*SpeedFac.x;
					  }
					  if (o.uv0.y > 0.5) {
						  o.posWorld.y = o.posWorld.y - (1 - distA) * 3 * (o.uv0.y - 0.5)*sin(o.posWorld.z + _Time.y);
					  }
				  }
				  /////////////////////////////////////////////////////////////////////////////////////////// END LOCAL INTERACTOR 2 /////////////////////////////////////////////
                               
                if( o.uv0.y > 0.1){
            //    	v.vertex.xyz += (node_133*(_BulgeScale*sin(_TimeControl1.w*_Time.y +_TimeControl1.z + dist) )*v.normal*(v.normal*_BulgeScale_copy)) / scaleY;
				}
                if( o.uv0.y >= 0.01){
           //     	v.vertex.y = v.vertex.y *_RandYScale* abs(cos(((_TimeControl1.w*_Time.y +_TimeControl1.z)*0.2 + 2*dist)*_RippleScale));
                }
                
                  //v1.5
	           _BulgeScale= _BulgeScale* _BulgeScale_copy;
	           //   _OceanCenter.x = 0.0;
	          // _OceanCenter.z = 0.0;

                  dist = 90* (cos(_BulgeShape+_Time.y/15))-_SmoothMotionFactor;
				///////////////////////// 
				if( o.uv0.y > 0.1){
					o.posWorld.x += _BulgeScale*1*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/5) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/5);
					o.posWorld.z += _BulgeScale*1*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/5) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/6);
				}
				if( o.uv0.y > 0.2){					
					o.posWorld.x += _BulgeScale*2*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/3) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/3);
					o.posWorld.z += _BulgeScale*2*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/3) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/3);	
				}
				if( o.uv0.y > 0.3){
					
					o.posWorld.x += _BulgeScale*3*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/3) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/4);
					o.posWorld.z += _BulgeScale*3*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/3) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/3);
				}
				if( o.uv0.y > 0.4){
					
					o.posWorld.x += _BulgeScale*4*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/2) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/2);
					o.posWorld.z += _BulgeScale*4*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/2) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/2);	
				}		
				if( o.uv0.y > 0.96){
					
					o.posWorld.x += _BulgeScale*5*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/0.9)	+ _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/1);
					o.posWorld.z += _BulgeScale*5*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/0.9) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/1);
				}

				//v2.1.13
				if (o.uv0.y > 0.16) {
					o.posWorld.y = o.posWorld.y * scaleYFactor.x + scaleYFactor.y;
				}

                //v2.2a     
                float2 windFromNoiseTex = tex2Dlod(_windTexture,
                    float4(o.posWorld.xz * _windTexture_ST.xy + (_noiseWindParams.xy * _noiseWindParams.w * _Time.y * 0.0001), 0.0, 0.0)).xy;
                float2 windTX = _noiseWindParams.z * (2.0 * windFromNoiseTex - 1);
                float noiseWindPow = length(windFromNoiseTex);
                if (o.uv0.y > 0.14) {
                    o.posWorld.xz += windTX;
                }

				
                

			 //v2.1.12 - 2.0.8
				if (_XTiles > 1 || _YTiles > 1) {
					float tilesX = _XTiles;
					float tilesY = _YTiles;
					float brushCount = tilesX * tilesY; //scale factor
					o.uv0.x = o.uv0.x / tilesX;
					o.uv0.y = o.uv0.y / tilesY;
					int brushID = 255 * texInteract.a;//clamp(255-texInteract.r*255,0,255)/brushCount;//e.g. 5

                    //v2.2
                   /* if (useNoisePattern != 0) {
                        brushID = 255 * texInteract.r * useNoisePattern;
                    }*/

					int division = (brushID + 1) / tilesX;//e.g. 2
					float ypoloipo = (brushID + 1) - (division*tilesX);//e.g. 5 - 2*2 = 5-4 = 1
					int sub = -1;
					if (ypoloipo > 0) {
						sub = 0;
					}
					//o.uv0.x = o.uv0.x + (ypoloipo - 1)*(1 / tilesX);
					//o.uv0.y = o.uv0.y + (division + sub)*(1 / tilesY);

                    //v2.2
                    if (useNoisePattern != 0) {
                        if (texInteract.a >= 0.75) {
                            o.uv0.x = o.uv0.x + 0.5;
                            o.uv0.y = o.uv0.y + 0.5;
                            brushID = 0;
                        }
                        else if (texInteract.a >= 0.5) {
                            o.uv0.x = o.uv0.x + 0.0;
                            o.uv0.y = o.uv0.y + 0.5;
                            brushID = 1;
                        }
                        else if (texInteract.a >= 0.25) {
                            o.uv0.x = o.uv0.x + 0.5;
                            o.uv0.y = o.uv0.y + 0.0;
                            brushID = 2;
                        }
                        else if (texInteract.a >= 0) {
                            o.uv0.x = o.uv0.x + 0.0;
                            o.uv0.y = o.uv0.y + 0.0;
                            brushID = 3;
                        }

                        //int brushID = 255 * texInteract.a;//clamp(255-texInteract.r*255,0,255)/brushCount;//e.g. 5
                       //division = (brushID + 1) / tilesX;//e.g. 2
                       //ypoloipo = (brushID + 1) - (division * tilesX);//e.g. 5 - 2*2 = 5-4 = 1
                       //sub = -1;
                       //if (ypoloipo > 0) {
                       //    sub = 0;
                       //}

                        float dispX = (ypoloipo - 1) * (1 / tilesX);
                        float dispY = (division + sub) * (1 / tilesY);
                        dispX = clamp(dispX, 0, 1);
                        dispY = clamp(dispY, 0, 1);
                        int timesX = (int)(dispX / 0.5);
                        int timesY = (int)(dispY / 0.5);
                        //o.uv0.x = o.uv0.x + timesX * 0.5;
                        //o.uv0.y = o.uv0.y + timesY * 0.5;
                        if (dispX != 0 || dispX != 0.25 || dispX != 0.5 || dispX != 1) {
                            dispX = 0;
                        }
                        if (dispY != 0 || dispY != 0.25 || dispY != 0.5 || dispY != 1) {
                            dispY = 0;
                        }
                        o.uv0.x = o.uv0.x + dispX;
                        o.uv0.y = o.uv0.y + dispY;

                        if (o.uv0.y > 0.16) {
                            //grassTypeHeights
                            o.posWorld.y = o.posWorld.y //grassTypeHeights.x * max(dispX,1) * max(dispY, 1);
                                + grassTypeHeights.x * max(dispX + dispY, 1) * (1 - texInteract.a) * (1 - 0.2 * pow(noiseWindPow, 1)) + grassTypeHeights.y * max(dispX + dispY, 1)
                                + grassTypeHeights.z * max(dispX + dispY, 1) + grassTypeHeights.w * max(dispX + dispY, 1);
                        }
                    }
                    else {
                        o.uv0.x = o.uv0.x + (ypoloipo - 1) * (1 / tilesX);
                        o.uv0.y = o.uv0.y + (division + sub) * (1 / tilesY);
                    }
				}

                //v2.2b
                float viewDot = dot(v.normal, UNITY_MATRIX_IT_MV[2].xyz);
                if (o.uv0.y > 0.05) {
                    o.posWorld.xz += UNITY_MATRIX_IT_MV[1].xz * _ViewBendStrength * saturate(viewDot);
                }

                //ADD GLOBAL ROTATION - WIND						
                v.vertex = mul(unity_WorldToObject, o.posWorld);
                //  v.vertex =  o.posWorld;
                
                //o.posWorld = mul(_Object2World, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                //UNITY_TRANSFER_SHADOW(o,v.uv0);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            
            fixed4 frag(VertexOutput i) : COLOR {
            
//                i.normalDir = normalize(i.normalDir);
//                float3x3 tangentTransform = float3x3( i.tangentDir, i.binormalDir, i.normalDir);
//                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
///////// Normals:
//                float2 node_583 = i.uv0;
//                float3 normalLocal = UnpackNormal(tex2D(_Normal,TRANSFORM_TEX(node_583.rg, _Normal))).rgb;
//                float3 normalDirection =  normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
//                
//                float nSign = sign( dot( viewDirection, i.normalDir ) ); // Reverse normal if this is a backface
//                i.normalDir *= nSign;
//                normalDirection *= nSign;
//                
//                float4 node_1 = tex2D(_Diffuse,TRANSFORM_TEX(node_583.rg, _Diffuse));
//                clip(node_1.a - _Cutoff);
//                
//                 //DEFINE FADE BASED ON CAMERA - INFINIGRASS
//				float Aplha = 1;
//				if(distance(i.posWorld, _WorldSpaceCameraPos) > _FadeThreshold){
//					 clip(-1);
//				}
//                
//                float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz,_WorldSpaceLightPos0.w));
//                float3 halfDirection = normalize(viewDirection+lightDirection);
//////// Lighting:
//                float attenuation = LIGHT_ATTENUATION(i);
//                
//                //INFINIGRASS
//                 float node_5027 = (_Time.y*_TimeControl1.x + _TimeEditor);
//                  float node_133 = pow((abs((frac((i.uv0+node_5027*float2(0.2,0.1)).r)-0.5))*2.0),_BulgeShape);
//                
//      //          float3 attenColor = attenuation * (_LightColor0.xyz*(node_133+1)  + _LightColor0.xyz*dot(viewDirection,lightDirection)/1);
//                
//                float3 attenColor = attenuation * _LightColor0.xyz ;
///////// Diffuse:
//                float NdotL = dot( normalDirection, lightDirection );
//                float3 w = float3(0.9,0.9,0.8)*0.5; // Light wrapping
//                float3 NdotLWrap = NdotL * ( 1.0 - w );
//                float3 forwardLight = max(float3(0.0,0.0,0.0), NdotLWrap + w );
//                float3 backLight = max(float3(0.0,0.0,0.0), -NdotLWrap + w ) ;//* float3(0.9,1,0.5); //v2.0.9
//                float3 diffuse = (forwardLight+backLight) * attenColor;
/////////// Gloss:
//                float gloss = 0.4;
//                float specPow = exp2( gloss * 10.0+1.0);
//////// Specular:
//                NdotL = max(0.0, NdotL);
//                float node_3 = 0.2;
//                float3 specularColor = float3(node_3,node_3,node_3)*node_1;//v2.0.9
//                float3 specular = attenColor * pow(max(0,dot(halfDirection,normalDirection)),specPow) * specularColor;
//                float3 finalColor = 0;
//                float3 diffuseLight = diffuse;
//                float node_331 = 1.0;
//                finalColor += float3(0,0,0);//diffuseLight * (lerp(float3(node_331,node_331,node_331),float3(0.9632353,0.8224623,0.03541304),i.vertexColor.b)*node_1.rgb);
//                
//                
//                //INFINIGRASS
//                //if( i.uv0.y > 0.6){
////                if(dot(viewDirection,lightDirection) > 0.9){
////                	finalColor += (i.uv0.y)/6 ;
////                }
//                //}
//                
//                
//                finalColor += specular ;
///// Final Color:
//
//				
//
//                return fixed4(finalColor * 1,0);


i.normalDir = normalize(i.normalDir);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.binormalDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
/////// Normals:
                float2 node_582 = i.uv0;
                float3 normalLocal = UnpackNormal(tex2D(_Normal,TRANSFORM_TEX(node_582.rg, _Normal))).rgb;
                float3 normalDirection =  normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                
                float nSign = sign( dot( viewDirection, i.normalDir ) ); // Reverse normal if this is a backface
                i.normalDir *= nSign;
                normalDirection *= nSign;
                
                float4 node_1 = tex2D(_Diffuse,TRANSFORM_TEX(node_582.rg, _Diffuse));
                
                
                
                clip(node_1.a - _Cutoff);
                
                //DEFINE FADE BASED ON CAMERA - INFINIGRASS
				float Aplha = 1;
				if(distance(i.posWorld, _WorldSpaceCameraPos) > _FadeThreshold){
					 clip(-1);
				}
                
//                #ifndef LIGHTMAP_OFF
//                    float4 lmtex = UNITY_SAMPLE_TEX2D(unity_Lightmap,i.uvLM);
//                    #ifndef DIRLIGHTMAP_OFF
//                        float3 lightmap = DecodeLightmap(lmtex);
//                        float3 scalePerBasisVector = DecodeLightmap(UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd,unity_Lightmap,i.uvLM));
//                        UNITY_DIRBASIS
//                        half3 normalInRnmBasis = saturate (mul (unity_DirBasis, normalLocal));
//                        lightmap *= dot (normalInRnmBasis, scalePerBasisVector);
//                    #else
//                        float3 lightmap = DecodeLightmap(lmtex);
//                    #endif
//                #endif
//                #ifndef LIGHTMAP_OFF
//                    #ifdef DIRLIGHTMAP_OFF
//                        float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
//                    #else
//                        float3 lightDirection = normalize (scalePerBasisVector.x * unity_DirBasis[0] + scalePerBasisVector.y * unity_DirBasis[1] + scalePerBasisVector.z * unity_DirBasis[2]);
//                        lightDirection = mul(lightDirection,tangentTransform); // Tangent to world
//                    #endif
//                #else
                    float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
               // #endif
                
                
                lightDirection =  normalize(reflect(_WorldSpaceLightPos0.xyz,normalDirection));
                
                
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                //float attenuation = 0;// LIGHT_ATTENUATION(i);
                UNITY_LIGHT_ATTENUATION(attenuation, i, i.posWorld.xyz);
                
                
                   //INFINIGRASS
                 float node_5027 = (_Time.y*_TimeControl1.x + _TimeEditor);
                 float node_133 = pow((abs((frac((i.uv0+node_5027*float2(0.2,0.1)).r)-0.5))*2.0),_BulgeShape);
                   float dist = distance(_OceanCenter, float3(_WaveControl1.x*i.posWorld.y,_WaveControl1.y*i.posWorld.x,_WaveControl1.z*i.posWorld.z) );
               
               float3 attenColor = attenuation * (_LightColor0.xyz);
               
               
               
/////// Diffuse:
                float NdotL = dot( normalDirection, lightDirection );
                float3 w = float3(0.9,0.9,0.8)*0.5; // Light wrapping
                float3 NdotLWrap = NdotL * ( 1.0 - w );
                float3 forwardLight = max(float3(0.0,0.0,0.0), NdotLWrap + w );
                float3 backLight = max(float3(0.0,0.0,0.0), -NdotLWrap + w ) ;//* float3(0.9,1,0.5); //v1.4


                //v2.0.9
//                #ifndef LIGHTMAP_OFF
//                    float3 diffuse = lightmap.rgb;
//                #else
//                    float3 diffuse = (forwardLight+backLight) * attenColor + UNITY_LIGHTMODEL_AMBIENT.rgb;
//                #endif


                //v2.0.9
                float3 diffuse = (forwardLight+backLight) * attenColor + half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);// UNITY_LIGHTMODEL_AMBIENT.rgb;

///////// Gloss:
                float gloss = 0.4;
                float specPow = exp2( gloss * 10.0+1.0);
////// Specular:
                NdotL = max(0.0, NdotL);
                float node_3 = 0.2;
                float3 specularColor = float3(node_3,node_3,node_3)*node_1;
                float3 specular = 3 * pow(max(0,dot(halfDirection,normalDirection)),specPow) * specularColor;
//                #ifndef LIGHTMAP_OFF
//                    #ifndef DIRLIGHTMAP_OFF
//                        specular *= lightmap;
//                    #else
//                        specular *= (floor(attenuation) * _LightColor0.xyz);
//                    #endif
//                #else
//                    specular *= (floor(attenuation) * _LightColor0.xyz);
//                #endif
                specular *= ((attenuation) * _LightColor0.xyz);
                float3 finalColor = 0;
                float3 diffuseLight = diffuse;
                float node_331 = 1.0;
             //   finalColor += diffuseLight * (lerp(float3(node_331,node_331,node_331),float3(0.9632353,0.8224623,0.03541304),i.vertexColor.b)*node_1.rgb);
				finalColor += diffuseLight * (node_1.rgb)*_ColorGlobal * 5 * attenuation; //v1.9.6 finalColor += diffuseLight * (node_1.rgb)*_ColorGlobal; //v1.4
                
                specular = specular * (i.uv0.y*2-0.5) ;
                finalColor = lerp(finalColor, finalColor*_Color,_TintPower*i.uv0.y*(0.9+0.6*cos(i.posWorld.x*2*_TintFrequency)+0.6*sin(i.posWorld.z*3*_TintFrequency)+0.6*sin(i.posWorld.z*1*_TintFrequency+0.1)));
                
                finalColor += specular * _SpecularPower;
/// Final Color:

				//SNOW
                float3 col = finalColor;
                
                float4 SnowTexColor = tex2D(_SnowTexture,  i.uv0);
				
				//if(i.uv0.y >= 1-(3 * (_SnowCoverage+_TimeControl1.y-1)) * col.r* col.r)
				//if(i.uv0.y >= 1-(4 * (_SnowCoverage+_TimeControl1.y-1)) * col.r* col.r* col.r+0.01) //v1.7.6
				if(i.uv0.y >= 1-(4 * (_SnowCoverage + _SnowOffset +_TimeControl1.y-1)) *clamp(col.r,0.85*(col.r+0.2),1)* clamp(col.r,0.35,1)* 1+0.01 ) //v2.0.7 
				//if(i.uv0.y >= 1-(4 * (_SnowCoverage+_TimeControl1.y-1)) *clamp(col.r,0.85*(col.r+0.2),1)* clamp(col.r,0.35,1)* 1+0.01 +  i.uv0.y*2+0.6  ) //v2.0.8
				//if(i.uv0.y >= 1-(4 * (_SnowCoverage+_TimeControl1.y-1)+  i.uv0.y*0.3 + 0.03 ) *clamp(col.r,0.85*(col.r+0.2),1)* clamp(col.r,0.35,1)* 1+0.01 +  i.uv0.y*0.3 + 0.03  ) //v2.0.8
	            {     	  
	            	if(i.uv0.y < 0.99 ){    //v1.7.6
	            		//if(i.uv0.y >=  1-(4 * (_SnowCoverage+_TimeControl1.y-1))*clamp(col.r,0.85*(col.r+0.2),1)* clamp(col.r,0.35,1)* 1+0.01 ){
			                //col =  lerp (  col , SnowTexColor*0.9,1-(0.5 * _SnowCoverage)) ;   
			                //    col = col * input.color * input.color.a *_UnityTerrainTreeTintColorSM *1.5;
			                //o.Normal = normalize(o.Normal + UnpackNormal(tex2D(_SnowBump, IN.uv_SnowBump))*1);   
			                //col.rgb = float4(i.uv0.y,i.uv0.y,i.uv0.y,1)*4*+_TimeControl1.z; 
			                //v1.7.6
			               // col.rgb = (float4(i.uv0.y,i.uv0.y,i.uv0.y,1)*4*+_TimeControl1.z)*(1+finalColor)*1.6;  
			               col.rgb = (float4(i.uv0.y,i.uv0.y,i.uv0.y,1)*4*+_TimeControl1.z)*(1+finalColor)*clamp(col.r,0.85*(col.r+0.2),1);   //v2.0.7
			              // col.rgb = float3(1,1,1)*2*(_TimeControl1.z+i.uv0.y*1)*clamp(col.r,0.35*(col.r+0.2),1);   //v2.0.8
		               //}
	                }                      
	            }
	            else
	            {
					//col = col * input.color * input.color.a *_UnityTerrainTreeTintColorSM *1.5;						
				//	col.rgb *= input.color.rgb;
				//	clip(col.a);
				//	col=col* _UnityTerrainTreeTintColorSM;
				}
                
                //END SNOW

                return fixed4(col * _localLightFactor,1);


            }
            ENDCG
        }
//        Pass {
//            Name "ShadowCollector"
//            Tags {
//                "LightMode"="ShadowCollector"
//            }
//            Cull Off
//            
//            Fog {Mode Off}
//            CGPROGRAM
//            #pragma vertex vert
//            #pragma fragment frag
//            #define UNITY_PASS_SHADOWCOLLECTOR
//            #define SHADOW_COLLECTOR_PASS
//            #include "UnityCG.cginc"
//            #include "Lighting.cginc"
//            #pragma fragmentoption ARB_precision_hint_fastest
//            #pragma multi_compile_shadowcollector
//            #pragma exclude_renderers gles xbox360 ps3 flash 
//            #pragma target 3.0
//            uniform float4 _TimeEditor;
//            #ifndef LIGHTMAP_OFF
//                // float4 unity_LightmapST;
//                // sampler2D unity_Lightmap;
//                #ifndef DIRLIGHTMAP_OFF
//                    // sampler2D unity_LightmapInd;
//                #endif
//            #endif
//            uniform sampler2D _Diffuse; uniform float4 _Diffuse_ST;
//            
//            uniform float _BulgeScale; 
//            uniform float _BulgeShape;
//            uniform float _BulgeScale_copy;
//            float4 _WaveControl1;
//   			float4 _TimeControl1;
//    		float3 _OceanCenter;
//    		uniform fixed _Cutoff;
//    		 uniform float _RandYScale;
//            uniform float _RippleScale;
//            
//            float3 _InteractPos;
//            float _FadeThreshold;
//            
//            struct VertexInput {
//                float4 vertex : POSITION;
//                float3 normal : NORMAL;
//                float2 texcoord0 : TEXCOORD0;
//                float4 vertexColor : COLOR;
//            };
//            struct VertexOutput {
//                V2F_SHADOW_COLLECTOR;
//                float2 uv0 : TEXCOORD5;
//                float3 normalDir : TEXCOORD6;
//                float4 vertexColor : COLOR;
//            };
//            VertexOutput vert (VertexInput v) {
//                VertexOutput o;
//                o.uv0 = v.texcoord0;
//                o.vertexColor = v.vertexColor;
//                o.normalDir = mul(float4(v.normal,0), _World2Object).xyz;
//                float4 node_389 = o.vertexColor;
//                float4 node_392 = _Time + _TimeEditor;
//            //    v.vertex.xyz += (normalize((float3(1,0.5,0.5)+v.normal))*node_389.r*sin(((node_389.b*3.141592654)+node_392.g+node_392.b))*0.16);
//                
//                  float dist = distance(_OceanCenter, float3(_WaveControl1.x*mul(_Object2World, v.vertex).y,_WaveControl1.y*mul(_Object2World, v.vertex).x,_WaveControl1.z*mul(_Object2World, v.vertex).z) );
//                float dist2 = distance(_OceanCenter, float3(mul(_Object2World, v.vertex).y,mul(_Object2World, v.vertex).x*0.10,0.1*mul(_Object2World, v.vertex).z) );
//                
//                float node_5027 = (_Time.y*_TimeControl1.x + _TimeEditor);//*sin(dist + 1.5*dist*pi);
//                float node_133 = pow((abs((frac((o.uv0+node_5027*float2(0.2,0.1)).r)-0.5))*2.0),_BulgeShape);
//                              
//                               //INIFNIGRASS
//                       float4 modelY = float4(0.0,1.0,0.0,0.0);
//                               float4 ModelYWorld =mul(_Object2World,modelY);
//                               float scaleY = length(ModelYWorld);
//                                 
//                if( o.uv0.y > 0.1){
//                	v.vertex.xyz += (node_133*(_BulgeScale*sin(_TimeControl1.w*_Time.y +_TimeControl1.z + dist) )*v.normal*(v.normal*_BulgeScale_copy)) /scaleY;
//				}
//				if( o.uv0.y >= 0.01){
//                	v.vertex.y = v.vertex.y *_RandYScale* abs(cos(((_TimeControl1.w*_Time.y +_TimeControl1.z)*0.2 + 2*dist)*_RippleScale));
//                }
//                
//                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
//                TRANSFER_SHADOW_COLLECTOR(o)
//                return o;
//            }
//            fixed4 frag(VertexOutput i) : COLOR {
//                i.normalDir = normalize(i.normalDir);
//                float2 node_584 = i.uv0;
//                float4 node_1 = tex2D(_Diffuse,TRANSFORM_TEX(node_584.rg, _Diffuse));
//                clip(node_1.a - _Cutoff);
//                
//                 //DEFINE FADE BASED ON CAMERA - INFINIGRASS
//				float Aplha = 1;
//				//if(distance(i.posWorld, _WorldSpaceCameraPos) > _FadeThreshold){
//				//	 clip(-1);
//				//}
//                
//                
//                SHADOW_COLLECTOR_FRAGMENT(i)
//            }
//            ENDCG
//        }
        Pass {
            Name "ShadowCaster"
            Tags {
                "LightMode"="ShadowCaster"
            }
            Cull Off
            Offset 1, 1
            
            Fog {Mode Off}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            //#define UNITY_PASS_SHADOWCASTER
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
        	//    #pragma exclude_renderers gles xbox360 ps3 flash 
			#pragma multi_compile_instancing //v1.7.8
			#pragma multi_compile_fwdbase nolightmap //v4.1
            #pragma target 3.0
            uniform float4 _TimeEditor;
            #ifndef LIGHTMAP_OFF
                // float4 unity_LightmapST;
                // sampler2D unity_Lightmap;
                #ifndef DIRLIGHTMAP_OFF
                    // sampler2D unity_LightmapInd;
                #endif
            #endif
            uniform sampler2D _Diffuse; uniform float4 _Diffuse_ST;
            
            //v2.2
            float useNoisePattern;
            float4 grassTypeHeights;
            //v2.2a     
            sampler2D _windTexture;
            float4 _windTexture_ST;
            uniform float4 _noiseWindParams;
            //v2.2b
            float _ViewBendStrength = 5;

			//v2.0.8
			sampler2D _InteractTexture;
			float3 _InteractTexturePos;
			//v2.1.1
			float _shapeOnlyHeight;
			//v2.1.12
			float _XTiles;
			float _YTiles;
			float _erasedShadowFactor;

			//v2.1.13
			float4 scaleYFactor;
			//v2.1.14
			float3 _InteractAmpFreqRad;
			float3 _InteractPos2;

            uniform float _BulgeScale; 
            uniform float _BulgeShape;
            uniform float _BulgeScale_copy;
            float4 _WaveControl1;
   			float4 _TimeControl1;
    		float3 _OceanCenter;
    		uniform fixed _Cutoff;
    		 uniform float _RandYScale;
            uniform float _RippleScale;
            
            float3 _InteractPos;
            float _FadeThreshold;
            float _StopMotionThreshold;
            float _SmoothMotionFactor;
            float _WaveXFactor;
           	float _WaveYFactor;

           	 float3 _InteractSpeed;
           	 float _InteractMaxYoffset;
            
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID //v1.7.8
            };
            struct VertexOutput {
                V2F_SHADOW_CASTER;
                float2 uv0 : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float4 vertexColor : COLOR;
                float4 posWorld : TEXCOORD3;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v); //v1.7.8
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.normalDir = mul(float4(v.normal,0), unity_WorldToObject).xyz;
                float4 node_389 = o.vertexColor;
                float4 node_392 = _Time + _TimeEditor;
              //  v.vertex.xyz += (normalize((float3(1,0.5,0.5)+v.normal))*node_389.r*sin(((node_389.b*3.141592654)+node_392.g+node_392.b))*0.16);
                

				//v2.0.8
				half2 tileableUv = mul(unity_ObjectToWorld, (v.vertex)).xz;
				float WorldScale = _InteractTexturePos.y;
				float3 CamPos = float3(0, 0, 0);//_DepthCameraPos;//_WorldSpaceCameraPos;
				float3 Origin = float3(_InteractTexturePos.x, _InteractTexturePos.z, 0);//float2(CamPos.x - WorldScale/2.0 , CamPos.z - WorldScale/2.0);
				float2 UnscaledTexPoint = float2(tileableUv.x - Origin.x, tileableUv.y - Origin.y);
				float2 ScaledTexPoint = float2(UnscaledTexPoint.x / WorldScale, UnscaledTexPoint.y / WorldScale);
				float4 texInteract = tex2Dlod(_InteractTexture, float4(ScaledTexPoint, 0.0, 0.0));



                  float dist = distance(_OceanCenter, float3(_WaveControl1.x*mul(unity_ObjectToWorld, v.vertex).y,_WaveControl1.y*mul(unity_ObjectToWorld, v.vertex).x,_WaveControl1.z*mul(unity_ObjectToWorld, v.vertex).z) );
                float dist2 = distance(_OceanCenter, float3(mul(unity_ObjectToWorld, v.vertex).y,mul(unity_ObjectToWorld, v.vertex).x*0.10,0.1*mul(unity_ObjectToWorld, v.vertex).z) );
                
                float node_5027 = (_Time.y*_TimeControl1.x + _TimeEditor);//*sin(dist + 1.5*dist*pi);
                float node_133 = pow((abs((frac((o.uv0+node_5027*float2(0.2,0.1)).r)-0.5))*2.0),_BulgeShape);
                            
                                //INIFNIGRASS
                       float4 modelY = float4(0.0,1.0,0.0,0.0);
                               float4 ModelYWorld =mul(unity_ObjectToWorld,modelY);
                               float scaleY = length(ModelYWorld);
                                  
                    o.posWorld = mul(unity_ObjectToWorld, v.vertex);
              // o.posWorld =  v.vertex;
                                      
//                if( distance(_InteractPos,o.posWorld) <  _StopMotionThreshold){                 
//                	_BulgeScale = 0;
//                	_BulgeScale_copy = 0;
//                }   

                    //v2.2
                    if (useNoisePattern != 0) {
                        //brushID = 255 * texInteract.r * useNoisePattern;
                        texInteract.a = texInteract.r * useNoisePattern;
                        texInteract.r = 0;
                        texInteract.g = 0;
                        texInteract.b = 0;
                    }

					//v.vertex = o.posWorld;
				//v2.0.8
					if (_shapeOnlyHeight == 0) { //v2.1.1
						o.posWorld.y = o.posWorld.y*clamp((1 - texInteract.r*texInteract.r* _erasedShadowFactor + 0.001)*1.5, 0, 1);
						o.posWorld.y = o.posWorld.y - texInteract.r * 2 * _erasedShadowFactor; //v2.1.14
						if (o.uv0.y > 0.01) {
							//o.posWorld.y = o.posWorld.y - texInteract.b * 2;

							if (texInteract.g >= 0.75) {
								o.posWorld.x = o.posWorld.x - (1 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
								o.posWorld.z = o.posWorld.z - (texInteract.g - 0.75)*o.uv0.y * 42 * texInteract.b;
							}
							else if (texInteract.g >= 0.5) {	//negative angle region					
								o.posWorld.x = o.posWorld.x - (texInteract.g - 0.5)*o.uv0.y * 42 * texInteract.b;
								o.posWorld.z = o.posWorld.z + (0.75 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
							}
							else if (texInteract.g >= 0.25) {
								o.posWorld.x = o.posWorld.x + (0.5 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
								o.posWorld.z = o.posWorld.z + (0.25 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
							}
							else if (texInteract.g > 0) {
								o.posWorld.x = o.posWorld.x + texInteract.g*o.uv0.y * 42 * texInteract.b;
								o.posWorld.z = o.posWorld.z + (0.25 - texInteract.g)*o.uv0.y * 42 * texInteract.b;
							}
						}

						//v2.1.16 - _grassNormal
						//v.vertex.y = v.vertex.y*_respectHeight+ (pow(255-tex.r*255,_ShoreFadeFactor) - 25*pow(1+1-tex.r,1.5) + 8  + o.uv0.y*_grassHeight ); 
						//if (_textureFeed == 1) { //v2.1.20b
						//	v.vertex.y = v.vertex.y*_respectHeight + (pow(942.64 - tex.r*942.64, _ShoreFadeFactor) - 0 + o.uv0.y*_grassHeight);
						///}
						//					        if(v.vertex.y > 25*_grassHeight){
						//					       		v.vertex.y =v.vertex.y*_respectHeight + o.uv0.y*_grassHeight + 8;
						//					        }

					}
					else {
						//v.vertex.y = v.vertex.y-texInteract.b*_shapeOnlyHeight*2;
						//v.vertex.y = abs(v.vertex.y)-pow(0+texInteract.r,_shapeOnlyHeight);
						//v.vertex.y = v.vertex.y*((texInteract.r+0.011)*12.5);
						o.posWorld.y = abs(o.posWorld.y) - sign(o.posWorld.y)*(1 - texInteract.r)*_shapeOnlyHeight;

						//if (_textureFeed == 1) { //v2.1.20b
						//	v.vertex.y = v.vertex.y*_respectHeight + (pow(255 - tex.r * 255, _ShoreFadeFactor) - 25 * pow(1 + 1 - tex.r, 1.5) + 8);
						//	v.vertex.y = abs(v.vertex.y)*_grassHeight;
						//}
					}
					//o.posWorld = v.vertex;



float3 SpeedFac = float3(0,0,0);  //	SpeedFac =  _InteractSpeed;  
                float distA =  distance(_InteractPos,o.posWorld)/ (_StopMotionThreshold*1);      
                  if( distance(_InteractPos,o.posWorld) < _StopMotionThreshold*1){ 
               // if( distance(_InteractPos.x,o.posWorld.x) < _StopMotionThreshold/2 ){    
               //      if( distance(_InteractPos.z,o.posWorld.z) < _StopMotionThreshold/2){                  
                	//_BulgeScale = 0;
                	//_BulgeScale_copy = 0;                	
                	SpeedFac =  _InteractSpeed * _WaveControl1.w;
//                		if( o.uv0.y > 0.2){
//							o.posWorld.x += (_InteractSpeed.x*22+0.1)*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/5) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/5);
//							o.posWorld.z += (_InteractSpeed.z+0.1)*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/5) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/6);
//						}
                	if( o.uv0.y < 0.3){
                	//_WaveXFactor = _WaveXFactor - (1-distA)*(1-distA)*SpeedFac.z;
                	//_WaveYFactor = _WaveYFactor - (1-distA)*(1-distA)*SpeedFac.x;
                	}
                	if( o.uv0.y > 0.19){
						_WaveXFactor = _WaveXFactor - (1-distA)*(1-distA)*SpeedFac.z*1;
                		_WaveYFactor = _WaveYFactor - (1-distA)*(1-distA)*SpeedFac.x*1;
                	}
                	if( o.uv0.y > 0.5){
						o.posWorld.y = o.posWorld.y - (1-distA)*_InteractMaxYoffset*(o.uv0.y-0.5)*sin(o.posWorld.z+_Time.y) ;//+_BulgeScale*0.5*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) ;
					}             
               }

				  //2.1.14
				  /////////////////////////////////////////////////////////////////////////////////////////// LOCAL INTERACTOR 2 /////////////////////////////////////////////
				  SpeedFac = float3(0, 0, 0);
				  distA = distance(_InteractPos2, o.posWorld) / (_InteractAmpFreqRad.z * 1);
				  if (distance(_InteractPos2, o.posWorld) < _InteractAmpFreqRad.z * 1) {
					  SpeedFac = 3 * (o.posWorld - _InteractPos2) *_WaveControl1.w
						  + _InteractAmpFreqRad.z*(1.1*cross(float3(0, 1, 0.5), o.posWorld - _InteractPos2) - 2.71*cross(float3(0, 1, 0), _InteractPos2 - o.posWorld))
						  + _InteractAmpFreqRad.x*(o.posWorld - _InteractPos2) *_WaveControl1.w *sin(o.posWorld.z + _InteractAmpFreqRad.y*_Time.y);
					  _BulgeScale = _BulgeScale * (1 / (distA + 0.01));

					  if (o.uv0.y < 0.3) {

					  }
					  if (o.uv0.y > 0.19) {
						  _WaveXFactor = _WaveXFactor - (1 - distA)*(1 - distA)*SpeedFac.z;
						  _WaveYFactor = _WaveYFactor - (1 - distA)*(1 - distA)*SpeedFac.x;
					  }
					  if (o.uv0.y > 0.5) {
						  o.posWorld.y = o.posWorld.y - (1 - distA) * 3 * (o.uv0.y - 0.5)*sin(o.posWorld.z + _Time.y);
					  }
				  }
				  /////////////////////////////////////////////////////////////////////////////////////////// END LOCAL INTERACTOR 2 /////////////////////////////////////////////
                                                                                         
                if( o.uv0.y > 0.1){
          //      	v.vertex.xyz += (node_133*(_BulgeScale*sin(_TimeControl1.w*_Time.y +_TimeControl1.z + dist) )*v.normal*(v.normal*_BulgeScale_copy)) / scaleY;
				}
				if( o.uv0.y >= 0.01){
          //      	v.vertex.y = v.vertex.y *_RandYScale* abs(cos(((_TimeControl1.w*_Time.y +_TimeControl1.z)*0.2 + 2*dist)*_RippleScale));
                }

                  //v1.5
	           _BulgeScale= _BulgeScale* _BulgeScale_copy;
	         //  _OceanCenter.x = 0.0;
	          // _OceanCenter.z = 0.0;
                
               dist = 90* (cos(_BulgeShape+_Time.y/15))-_SmoothMotionFactor;
				///////////////////////// 
				if( o.uv0.y > 0.1){
					o.posWorld.x += _BulgeScale*1*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/5) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/5);
					o.posWorld.z += _BulgeScale*1*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/5) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/6);
				}
				if( o.uv0.y > 0.2){					
					o.posWorld.x += _BulgeScale*2*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/3) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/3);
					o.posWorld.z += _BulgeScale*2*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/3) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/3);	
				}
				if( o.uv0.y > 0.3){
					
					o.posWorld.x += _BulgeScale*3*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/3) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/4);
					o.posWorld.z += _BulgeScale*3*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/3) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/3);
				}
				if( o.uv0.y > 0.4){
					
					o.posWorld.x += _BulgeScale*4*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/2) + _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/2);
					o.posWorld.z += _BulgeScale*4*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/2) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/2);	
				}		
				if( o.uv0.y > 0.96){
					
					o.posWorld.x += _BulgeScale*5*cos(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*sin(o.posWorld.z+_Time.y) + _WaveXFactor*((2+cos(o.posWorld.x/dist))*_OceanCenter.x/0.9)	+ _WaveYFactor*((3+sin(2*o.posWorld.z/dist))*_OceanCenter.z/1);
					o.posWorld.z += _BulgeScale*5*sin(o.posWorld.x*_WaveControl1.x+_Time.y*_TimeControl1.x + o.posWorld.z*_WaveControl1.z)*0.1*cos(o.posWorld.z+_Time.y) + _WaveXFactor*((2+sin(o.posWorld.z/dist))*_OceanCenter.z/0.9) + _WaveYFactor*((3+cos(3*o.posWorld.x/dist))*_OceanCenter.x/1);
				}

				//v2.1.13
				if (o.uv0.y > 0.16) {
					o.posWorld.y = o.posWorld.y * scaleYFactor.x + scaleYFactor.y;
				}

                //v2.2a     
                float2 windFromNoiseTex = tex2Dlod(_windTexture,
                    float4(o.posWorld.xz * _windTexture_ST.xy + (_noiseWindParams.xy * _noiseWindParams.w * _Time.y * 0.0001), 0.0, 0.0)).xy;
                float2 windTX = _noiseWindParams.z * (2.0 * windFromNoiseTex - 1);
                float noiseWindPow = length(windFromNoiseTex);
                if (o.uv0.y > 0.14) {
                    o.posWorld.xz += windTX;
                }


                
                
				 //v2.1.12 - 2.0.8
				if (_XTiles > 1 || _YTiles > 1) {
					float tilesX = _XTiles;
					float tilesY = _YTiles;
					float brushCount = tilesX * tilesY; //scale factor
					o.uv0.x = o.uv0.x / tilesX;
					o.uv0.y = o.uv0.y / tilesY;
					int brushID = 255 * texInteract.a;//clamp(255-texInteract.r*255,0,255)/brushCount;//e.g. 5
					int division = (brushID + 1) / tilesX;//e.g. 2
					float ypoloipo = (brushID + 1) - (division*tilesX);//e.g. 5 - 2*2 = 5-4 = 1
					int sub = -1;
					if (ypoloipo > 0) {
						sub = 0;
					}
					//o.uv0.x = o.uv0.x + (ypoloipo - 1)*(1 / tilesX);
					//o.uv0.y = o.uv0.y + (division + sub)*(1 / tilesY);

                    //v2.2
                    if (useNoisePattern != 0) {
                        if (texInteract.a >= 0.75) {
                            o.uv0.x = o.uv0.x + 0.5;
                            o.uv0.y = o.uv0.y + 0.5;
                            brushID = 0;
                        }
                        else if (texInteract.a >= 0.5) {
                            o.uv0.x = o.uv0.x + 0.0;
                            o.uv0.y = o.uv0.y + 0.5;
                            brushID = 1;
                        }
                        else if (texInteract.a >= 0.25) {
                            o.uv0.x = o.uv0.x + 0.5;
                            o.uv0.y = o.uv0.y + 0.0;
                            brushID = 2;
                        }
                        else if (texInteract.a >= 0) {
                            o.uv0.x = o.uv0.x + 0.0;
                            o.uv0.y = o.uv0.y + 0.0;
                            brushID = 3;
                        }

                        //int brushID = 255 * texInteract.a;//clamp(255-texInteract.r*255,0,255)/brushCount;//e.g. 5
                       //division = (brushID + 1) / tilesX;//e.g. 2
                       //ypoloipo = (brushID + 1) - (division * tilesX);//e.g. 5 - 2*2 = 5-4 = 1
                       //sub = -1;
                       //if (ypoloipo > 0) {
                       //    sub = 0;
                       //}

                        float dispX = (ypoloipo - 1) * (1 / tilesX);
                        float dispY = (division + sub) * (1 / tilesY);
                        dispX = clamp(dispX, 0, 1);
                        dispY = clamp(dispY, 0, 1);
                        int timesX = (int)(dispX / 0.5);
                        int timesY = (int)(dispY / 0.5);
                        //o.uv0.x = o.uv0.x + timesX * 0.5;
                        //o.uv0.y = o.uv0.y + timesY * 0.5;
                        if (dispX != 0 || dispX != 0.25 || dispX != 0.5 || dispX != 1) {
                            dispX = 0;
                        }
                        if (dispY != 0 || dispY != 0.25 || dispY != 0.5 || dispY != 1) {
                            dispY = 0;
                        }
                        o.uv0.x = o.uv0.x + dispX;
                        o.uv0.y = o.uv0.y + dispY;

                        if (o.uv0.y > 0.16) {
                            //grassTypeHeights
                            o.posWorld.y = o.posWorld.y //grassTypeHeights.x * max(dispX,1) * max(dispY, 1);
                                + grassTypeHeights.x * max(dispX + dispY, 1) * (1 - texInteract.a) * (1 - 0.2 * pow(noiseWindPow, 1)) + grassTypeHeights.y * max(dispX + dispY, 1)
                                + grassTypeHeights.z * max(dispX + dispY, 1) + grassTypeHeights.w * max(dispX + dispY, 1);
                        }
                    }
                    else {
                        o.uv0.x = o.uv0.x + (ypoloipo - 1) * (1 / tilesX);
                        o.uv0.y = o.uv0.y + (division + sub) * (1 / tilesY);
                    }
				}

                //v2.2b
                float viewDot = dot(v.normal, UNITY_MATRIX_IT_MV[2].xyz);
                if (o.uv0.y > 0.05) {
                    o.posWorld.xz += UNITY_MATRIX_IT_MV[1].xz * _ViewBendStrength * saturate(viewDot);
                }

                //ADD GLOBAL ROTATION - WIND						
                v.vertex = mul(unity_WorldToObject, o.posWorld);
                //v.vertex =  o.posWorld;
                
                o.pos = UnityObjectToClipPos(v.vertex);
               // o.posWorld = mul(_Object2World, v.vertex);
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            half4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float2 node_585 = i.uv0;
                float4 node_1 = tex2D(_Diffuse,TRANSFORM_TEX(node_585.rg, _Diffuse));
                clip(node_1.a - _Cutoff);
                
                 //DEFINE FADE BASED ON CAMERA - INFINIGRASS
				float Aplha = 1;
				if(distance(i.posWorld, _WorldSpaceCameraPos) > _FadeThreshold){
					 clip(-1);
				}
                
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Transparent/Cutout/Diffuse"
   
}