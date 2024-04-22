using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SSProbeKeywords
{
    public static readonly int RTAS_ID = Shader.PropertyToID("_RaytracingAccelerationStructure");

    public static readonly int CAMERA_POS_WS_ID = Shader.PropertyToID("_CameraPosWS");
    public static readonly int CAMERA_INV_VIEW_PROJECT_ID = Shader.PropertyToID("_CameraInvViewProj");

    #region Textures

    public static readonly int SAMPLE_RAYTRACE_TEX_ID = Shader.PropertyToID("_SampleRaytraceTex");

    #endregion

    #region MyRegion

    public const string SAMPLE_RAY_TRACING_PASS_NAME = "SampleRaytracing";

    #endregion
}