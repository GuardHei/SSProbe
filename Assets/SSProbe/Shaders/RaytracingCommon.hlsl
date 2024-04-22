#ifndef RT_RAYTRACING_COMMON_INCLUDED
#define RT_RAYTRACING_COMMON_INCLUDED

#include <UnityRayQuery.cginc>
#include <UnityRayTracingMeshUtils.cginc>
#include <Assets/SSProbe/Shaders/ShaderVariablesRaytracing.hlsl>

static const uint kRayFlagNone                      = 0x00;
static const uint kRayFlagForceOpaque               = 0x01;
static const uint kRayFlagForceNonOpaque            = 0x02;
static const uint kRayFlagAcceptFirstHitAndEndSearch = 0x04;
static const uint kRayFlagSkipClosetHitShader       = 0x08;
static const uint kRayFlagCullBackFacingTriangles   = 0x10;
static const uint kRayFlagCullFrontFacingTriangles  = 0x20;
static const uint kRayFlagCullOpaque                = 0x40;
static const uint kRayFlagCullNonOpaque             = 0x80;
static const uint kRayFlagSkipTriangles             = 0x100;
static const uint kRayFlagSkipProceduralPrimitives  = 0x200;

static const uint kAllInstanceMask = 0xFFFFFFF;

#define COLOR_MAGENTA float4(1, 0, 1, 1)

struct AttributeData
{
    float2 barycentrics;
};

struct SamplePayload
{
    float4 color;
};

struct DiffuseGIPayload
{
    float4 pack0; // rgb: albedo, alpha: metallic
    float4 pack1; // rgb: normal, alpha: smoothness
    float4 pack2; // rgb: emissive, alpha: ~
};

struct SampleVertexData
{
    float3 positionOS;
    float3 normalOS;
    float2 texcoord0;
};

float3 _CameraPosWS;
float4x4 _CameraInvViewProj;

RWTexture2D<float4> _SampleRaytraceTex;

float2 RayIdToScreenPos(uint2 id, uint2 dimensions)
{
    float2 xy = id + 0.5f;
    float2 screenPos = xy / dimensions * 2.0f - 1.0f;
    return screenPos;
}

void GenerateCameraRay(float2 screenPos, out float3 origin, out float3 direction)
{
    // center in the middle of the pixel.
    // float2 xy = DispatchRaysIndex().xy + 0.5f;
    // float2 screenPos = xy / DispatchRaysDimensions().xy * 2.0f - 1.0f;

    // Un project the pixel coordinate into a ray.
    float4 world = mul(_CameraInvViewProj, float4(screenPos, 0, 1));

    world.xyz /= world.w;
    origin = _CameraPosWS.xyz;
    direction = normalize(world.xyz - origin);
}

float3 GetFullBarycentrics(float2 barycentrics)
{
    return float3(1.0f - barycentrics.x - barycentrics.y, barycentrics.x, barycentrics.y);
}

float BarycentricInterpolate(float v0, float v1, float v2, float3 bary)
{
    return v0 * bary.x + v1 * bary.y + v2 * bary.z;
}

float2 BarycentricInterpolate(float2 v0, float2 v1, float2 v2, float3 bary)
{
    return v0 * bary.x + v1 * bary.y + v2 * bary.z;
}

float3 BarycentricInterpolate(float3 v0, float3 v1, float3 v2, float3 bary)
{
    return v0 * bary.x + v1 * bary.y + v2 * bary.z;
}

float4 BarycentricInterpolate(float4 v0, float4 v1, float4 v2, float3 bary)
{
    return v0 * bary.x + v1 * bary.y + v2 * bary.z;
}

uint3 GetRaytracingTriangleIndices()
{
    uint3 triangleIndices =  UnityRayTracingFetchTriangleIndices(PrimitiveIndex());
    return triangleIndices;
}

void FetchRaytracingSampleVertexData(uint index, inout SampleVertexData data)
{
    data.positionOS = UnityRayTracingFetchVertexAttribute3(index, kVertexAttributePosition);
    data.normalOS = UnityRayTracingFetchVertexAttribute3(index, kVertexAttributeNormal);
    data.texcoord0 = UnityRayTracingFetchVertexAttribute2(index, kVertexAttributeTexCoord0);
}

#endif