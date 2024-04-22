#ifndef RT_RAYTRACING_SAMPLE_CLOSEST_HIT_INCLUDED
#define RT_RAYTRACING_SAMPLE_CLOSEST_HIT_INCLUDED

#include <Assets/SSProbe/Shaders/RaytracingCommon.hlsl>
#include <Packages/com.unity.render-pipelines.universal@16.0.5/Shaders/LitInput.hlsl>

/*
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
float4 _BaseMap_TexelSize;
float4 _BaseMap_MipInfo;
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
float4 _DetailAlbedoMap_ST;
half4 _BaseColor;
half4 _SpecColor;
half4 _EmissionColor;
half _Cutoff;
half _Smoothness;
half _Metallic;
half _BumpScale;
half _Parallax;
half _OcclusionStrength;
half _ClearCoatMask;
half _ClearCoatSmoothness;
half _DetailAlbedoMapScale;
half _DetailNormalMapScale;
half _Surface;
CBUFFER_END
*/

float4 RT_SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
    return float4(SAMPLE_TEXTURE2D_LOD(albedoAlphaMap, sampler_albedoAlphaMap, uv, 0));
}

float3 RT_SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), float scale = 1.0f)
{
    #ifdef _NORMALMAP
    float4 n = SAMPLE_TEXTURE2D_LOD(bumpMap, sampler_bumpMap, uv, 0);
    #if BUMP_SCALE_NOT_SUPPORTED
    return UnpackNormal(n);
    #else
    return UnpackNormalScale(n, scale);
    #endif
    #else
    return float3(.0f, .0f, 1.0f);
    #endif
}

float3 RT_SampleEmission(float2 uv, half3 emissionColor, TEXTURE2D_PARAM(emissionMap, sampler_emissionMap))
{
    #ifndef _EMISSION
    return 0;
    #else
    return SAMPLE_TEXTURE2D_LOD(emissionMap, sampler_emissionMap, uv, 0).rgb * emissionColor;
    #endif
}

[shader("closesthit")]
void SampleRayClosestHit(inout SamplePayload payload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // payload.color = float4(1.0f, 1.0f, .0f, 1.0f);
    // return;
    
    uint3 triangleIndice = GetRaytracingTriangleIndices();

    SampleVertexData v0, v1, v2;
    FetchRaytracingSampleVertexData(triangleIndice.x, v0);
    FetchRaytracingSampleVertexData(triangleIndice.y, v1);
    FetchRaytracingSampleVertexData(triangleIndice.z, v2);

    float3 bary = GetFullBarycentrics(attributeData.barycentrics);

    // payload.color = float4(bary, 1.0f);
    // return;
    
    float3 normalOS = BarycentricInterpolate(v0.normalOS, v1.normalOS, v2.normalOS, bary);
    float3x3 objectToWorld = (float3x3) ObjectToWorld3x4();
    float3 normalWS = normalize(mul(objectToWorld, normalOS));

    float2 texcoord0 = BarycentricInterpolate(v0.texcoord0, v1.texcoord0, v2.texcoord0, bary);
    float2 uv = texcoord0;

    float3 albedo = RT_SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).rgb;
    albedo = albedo * _BaseColor.rgb;

    float3 normalTS = RT_SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

    float3 emission = RT_SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

    float3 lighting = albedo + emission;
    
    payload.color = float4(lighting, 1.0f);
}

#endif