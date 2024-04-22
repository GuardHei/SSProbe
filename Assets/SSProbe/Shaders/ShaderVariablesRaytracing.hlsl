#ifndef RT_SHADER_VARIABLES_RAYTRACING_INCLUDED
#define RT_SHADER_VARIABLES_RAYTRACING_INCLUDED

#include <Packages/com.unity.render-pipelines.core@16.0.5/ShaderLibrary/Common.hlsl>

#define RAY_TRACING_ACCELERATION_STRUCTURE_REGISTER t0

#define RTAS _RaytracingAccelerationStructure

GLOBAL_RESOURCE(RaytracingAccelerationStructure, _RaytracingAccelerationStructure, RAY_TRACING_ACCELERATION_STRUCTURE_REGISTER);

#endif
