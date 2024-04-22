using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleRendererFeature("Sample Raytrace Pass")]
public class SampleRaytrace : ScriptableRendererFeature
{
    
    [SerializeField]
    private RayTracingShader m_Shader;
    private SampleRaytracePass m_SampleRaytracePass;
    
    class SampleRaytracePass : ScriptableRenderPass
    {
        private CommandBuffer m_Cmd = new()
        {
            name = "---- Sample Raytrace ----"
        };

        private RayTracingShader m_Shader;

        private RTHandleSystem m_RTHandleSystem;
        
        private RTHandle m_SampleRaytraceTex;

        public SampleRaytracePass()
        {
            profilingSampler = new ProfilingSampler(nameof(SampleRaytracePass));
            m_RTHandleSystem = new RTHandleSystem();
            m_RTHandleSystem.Initialize(Screen.width, Screen.height);
        }
        
        ~SampleRaytracePass()
        {
            m_RTHandleSystem?.Dispose();
        }

        public void Setup(RayTracingShader shader)
        {
            m_Shader = shader;
        }
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var cam = renderingData.cameraData.camera;
            var camRes = new Vector2Int(cam.scaledPixelWidth, cam.scaledPixelHeight);
            
            m_RTHandleSystem.ResetReferenceSize(camRes.x, camRes.y);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!m_Shader)
            {
                Debug.LogWarning("SampleRaytracing shader is not set!");
                return;
            }
            
            using (new ProfilingScope(m_Cmd, profilingSampler))
            {
                m_Cmd.Clear();
                
                var cam = renderingData.cameraData.camera;
                var camRes = new Vector2Int(cam.scaledPixelWidth, cam.scaledPixelHeight);

                var viewMat = renderingData.cameraData.GetViewMatrix();
                var gpuProjMat = renderingData.cameraData.GetGPUProjectionMatrix();
                var vpMat = gpuProjMat * viewMat;
                var invVpMat = vpMat.inverse;
                
                var sampleRaytraceTexDesc = new RenderTextureDescriptor(cam.scaledPixelWidth, cam.scaledPixelHeight, GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormat.None)
                {
                    sRGB = false, 
                    depthBufferBits = 0,
                    dimension = TextureDimension.Tex2D,
                    enableRandomWrite = true,
                    useMipMap = false,
                    autoGenerateMips = false,
                    useDynamicScale = false,
                    mipCount = 1,
                    bindMS = false,
                    msaaSamples = 1,
                    vrUsage = VRTextureUsage.None,
                    memoryless = RenderTextureMemoryless.None
                };

                m_SampleRaytraceTex = RTHandles.Alloc(sampleRaytraceTexDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, false, 1, .0f, "_RTAlloc_Sample");

                // var rt = m_SampleRaytraceTex.rt;
                // Debug.Log(m_SampleRaytraceTex.name + " " + rt.enableRandomWrite + " " + rt.format + " " + rt.graphicsFormat);
                // Debug.Log(rt.dimension + " " + rt.width + " " + rt.height);
                
                m_Cmd.GetTemporaryRT(SSProbeKeywords.SAMPLE_RAYTRACE_TEX_ID, sampleRaytraceTexDesc, FilterMode.Bilinear);
                
                m_Cmd.SetGlobalVector(SSProbeKeywords.CAMERA_POS_WS_ID, renderingData.cameraData.worldSpaceCameraPos);
                m_Cmd.SetGlobalMatrix(SSProbeKeywords.CAMERA_INV_VIEW_PROJECT_ID, invVpMat);
                
                m_Cmd.SetRayTracingTextureParam(m_Shader, SSProbeKeywords.SAMPLE_RAYTRACE_TEX_ID, SSProbeKeywords.SAMPLE_RAYTRACE_TEX_ID);
                m_Cmd.SetRayTracingShaderPass(m_Shader, SSProbeKeywords.SAMPLE_RAY_TRACING_PASS_NAME);

                var rtSize = camRes;
                var dispatchDim = new uint3((uint) rtSize.x, (uint) rtSize.y, 1u);
                m_Cmd.DispatchRays(m_Shader, "SampleRayGen", dispatchDim.x, dispatchDim.y, dispatchDim.z, cam);
                
                context.ExecuteCommandBuffer(m_Cmd);
            }
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (m_SampleRaytraceTex != null)
            {
                RTHandles.Release(m_SampleRaytraceTex);
                m_SampleRaytraceTex = null;
            }
        }
    }

    /// <inheritdoc/>
    public override void Create()
    {
        m_SampleRaytracePass = new SampleRaytracePass
        {
            // Configures where the render pass should be injected.
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques
        };
        
        m_SampleRaytracePass.Setup(m_Shader);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_SampleRaytracePass);
    }
}


