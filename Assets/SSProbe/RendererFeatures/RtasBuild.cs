using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

[DisallowMultipleRendererFeature("Raytracing Accel Struct Build Pass")]
[Tooltip("Build Acceleration Structure for raytracing effects.")]
public class RtasBuild : ScriptableRendererFeature
{
    [SerializeField]
    private RayTracingAccelerationStructure.ManagementMode m_ManagementMode = RayTracingAccelerationStructure.ManagementMode.Automatic;
    
    [SerializeField]
    private RayTracingAccelerationStructure.RayTracingModeMask m_RayTracingModeMask = RayTracingAccelerationStructure.RayTracingModeMask.Everything;
    
    [SerializeField]
    private LayerMask m_LayerMask = -1;

    [SerializeField]
    private RayTracingAccelerationStructureBuildFlags m_BuildFlagsStaticGeo = RayTracingAccelerationStructureBuildFlags.PreferFastTrace;
    
    [SerializeField]
    private RayTracingAccelerationStructureBuildFlags m_BuildFlagsDynamicGeo = RayTracingAccelerationStructureBuildFlags.PreferFastTrace;
    
    private RtasBuildPass m_RtasBuildPass;
    private RayTracingAccelerationStructure m_Rtas;
    
    class RtasBuildPass : ScriptableRenderPass
    {
        private RayTracingAccelerationStructure m_Rtas;

        public RtasBuildPass()
        {
            profilingSampler = new ProfilingSampler(nameof(RtasBuildPass));
        }

        public void Setup(RayTracingAccelerationStructure rtas)
        {
            m_Rtas = rtas;
        }
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var cam = renderingData.cameraData.camera;
            
            RTHandles.ResetReferenceSize(cam.scaledPixelWidth, cam.scaledPixelHeight);
            
            if (m_Rtas == null)
            {
                Debug.LogError("RTAS is not built!");
                return;
            }
            
            /*
            Profiler.BeginSample("---- RTAccelBuild.OnCameraSetup ----");

            m_Rtas.Build();
            
            Profiler.EndSample();
            */

            using (new ProfilingScope(cmd, profilingSampler))
            {
                cmd.BuildRayTracingAccelerationStructure(m_Rtas);
                cmd.SetGlobalRayTracingAccelerationStructure(SSProbeKeywords.RTAS_ID, m_Rtas);
            }
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }
    }

    /// <inheritdoc/>
    public override void Create()
    {
        var buildSettings = new RayTracingAccelerationStructure.Settings(m_ManagementMode, m_RayTracingModeMask, m_LayerMask, m_BuildFlagsStaticGeo, m_BuildFlagsDynamicGeo);
        m_Rtas?.Dispose();
        m_Rtas = new RayTracingAccelerationStructure(buildSettings);
        m_RtasBuildPass = new RtasBuildPass
        {
            // Configures where the render pass should be injected.
            renderPassEvent = RenderPassEvent.BeforeRendering
        };
        
        m_RtasBuildPass.Setup(m_Rtas);
    }

    private void Reset()
    {
        m_Rtas?.Dispose();
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_RtasBuildPass);
    }
    
    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        m_Rtas?.Dispose();
        m_Rtas = null;
        m_RtasBuildPass = null;
    }
}


