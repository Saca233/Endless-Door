//RealToon - DeNorSob Outline Effect (URP - Post Processing)
//©MJQStudioWorks

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class DeNorSobOutlineRendererFeature : ScriptableRendererFeature
{
    #region FEATURE_FIELDS

    [SerializeField] [HideInInspector] private Shader m_Shader;
    private Material m_Material;
    private DeNorSobOutlineRenderPass m_FullScreenPass;

    public RenderPassEvent InjectionPoint = RenderPassEvent.BeforeRenderingTransparents;
    private static LayerMask layersToExclude;

    #endregion

    #region FEATURE_METHODS

    public override void Create()
    {
        if (m_Shader == null)
            m_Shader = Shader.Find("Hidden/URP/RealToon/Effects/DeNorSobOutline");

        if (m_Material == null)
            m_Material = CoreUtils.CreateEngineMaterial(m_Shader);

        if (m_Material)
            m_FullScreenPass = new DeNorSobOutlineRenderPass(name, m_Material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview || renderingData.cameraData.cameraType == CameraType.Reflection)
            return;

        var stack = VolumeManager.instance.stack;
        if (stack == null) return;

        DeNorSobOutlineVolumeComponent myVolume = stack.GetComponent<DeNorSobOutlineVolumeComponent>();

        if (myVolume == null || !myVolume.IsActive())
        {
            m_Material.SetFloat("_OutlineWidth", 0f);
            return;
        }

        m_Material.SetFloat("_OutlineWidth", myVolume.OutlineWidth.value);

        m_FullScreenPass.renderPassEvent = InjectionPoint;
        m_FullScreenPass.ConfigureInput(ScriptableRenderPassInput.Normal);
        renderer.EnqueuePass(m_FullScreenPass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);

        m_Material = null;
        m_FullScreenPass = null;
    }

    #endregion

    private class DeNorSobOutlineRenderPass : ScriptableRenderPass
    {
        #region PASS_FIELDS

        private Material m_Material;
        private RTHandle m_CopiedColor;

        private static MaterialPropertyBlock s_SharedPropertyBlock = new MaterialPropertyBlock();

        private static readonly bool kCopyActiveColor = true;
        private static readonly bool kBindDepthStencilAttachment = true;

        private static readonly int kBlitTexturePropertyId = Shader.PropertyToID("_BlitTexture");
        private static readonly int kBlitScaleBiasPropertyId = Shader.PropertyToID("_BlitScaleBias");

        private List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
        public static FilteringSettings filteringSettings;

        #endregion

        public DeNorSobOutlineRenderPass(string passName, Material material)
        {
            profilingSampler = new ProfilingSampler(passName);
            m_Material = material;

            requiresIntermediateTexture = kCopyActiveColor;

            shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));

        }

        #region PASS_SHARED_RENDERING_CODE

        private static void ExecuteCopyColorPass(RasterCommandBuffer cmd, RTHandle sourceTexture)
        {
            Blitter.BlitTexture(cmd, sourceTexture, new Vector4(1, 1, 0, 0), 0.0f, false);
        }

        private static void ExecuteMainPass(RasterCommandBuffer cmd, RTHandle sourceTexture, Material material)
        {
            s_SharedPropertyBlock.Clear();
            if(sourceTexture != null)
                s_SharedPropertyBlock.SetTexture(kBlitTexturePropertyId, sourceTexture);

            s_SharedPropertyBlock.SetVector(kBlitScaleBiasPropertyId, new Vector4(1, 1, 0, 0));

            //
            DeNorSobOutlineVolumeComponent myVolume = VolumeManager.instance.stack?.GetComponent<DeNorSobOutlineVolumeComponent>();
            if (myVolume != null)

                s_SharedPropertyBlock.SetFloat("_OutlineWidth", myVolume.OutlineWidth.value);

                s_SharedPropertyBlock.SetFloat("_DepthThreshold", myVolume.DepthThreshold.value);

                s_SharedPropertyBlock.SetFloat("_NormalThreshold", myVolume.NormalThreshold.value);
                s_SharedPropertyBlock.SetFloat("_NormalMin", myVolume.NormalMin.value);
                s_SharedPropertyBlock.SetFloat("_NormalMax", myVolume.NormalMax.value);

                s_SharedPropertyBlock.SetFloat("_SobOutSel", myVolume.SobelOutline.value ? 1 : 0);
                s_SharedPropertyBlock.SetFloat("_SobelOutlineThreshold", myVolume.SobelOutlineThreshold.value);
                s_SharedPropertyBlock.SetFloat("_WhiThres", 1.0f - myVolume.WhiteThreshold.value);
                s_SharedPropertyBlock.SetFloat("_BlaThres", myVolume.BlackThreshold.value);

                s_SharedPropertyBlock.SetColor("_OutlineColor", myVolume.OutlineColor.value);
                s_SharedPropertyBlock.SetFloat("_OutlineColorIntensity", myVolume.ColorIntensity.value);
                s_SharedPropertyBlock.SetFloat("_ColOutMiSel", myVolume.MixFullScreenColor.value ? 1 : 0);

                s_SharedPropertyBlock.SetFloat("_OutOnSel", myVolume.ShowOutlineOnly.value ? 1 : 0);

                s_SharedPropertyBlock.SetFloat("_MixDeNorSob", myVolume.MixDephNormalAndSobelOutline.value ? 1 : 0);

                layersToExclude = myVolume.LayersToExclude.value;

                switch (myVolume.SobelOutline.value)
                {
                    case true:
                        material.EnableKeyword("RENDER_OUTLINE_ALL");
                        break;
                    default:
                        material.DisableKeyword("RENDER_OUTLINE_ALL");
                        break;
                }

                switch (myVolume.MixDephNormalAndSobelOutline.value)
                {
                    case true:
                        material.EnableKeyword("MIX_DENOR_SOB");
                        break;
                    default:
                        material.DisableKeyword("MIX_DENOR_SOB");
                        break;
                }

            cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1, s_SharedPropertyBlock);
        }

        private static RenderTextureDescriptor GetCopyPassTextureDescriptor(RenderTextureDescriptor desc)
        {
            desc.msaaSamples = 1;

            desc.depthBufferBits = (int)DepthBits.None;

            return desc;
        }

        #endregion

        #region PASS_NON_RENDER_GRAPH_PATH

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {

            ResetTarget();

            if (kCopyActiveColor)
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_CopiedColor, GetCopyPassTextureDescriptor(renderingData.cameraData.cameraTargetDescriptor), name: "_DeNorSobOutlineCopyColor");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, profilingSampler))
            {
                RasterCommandBuffer rasterCmd = CommandBufferHelpers.GetRasterCommandBuffer(cmd);
                if (kCopyActiveColor)
                {
                    CoreUtils.SetRenderTarget(cmd, m_CopiedColor);
                    ExecuteCopyColorPass(rasterCmd, cameraData.renderer.cameraColorTargetHandle);
                }

                if(kBindDepthStencilAttachment)
                    CoreUtils.SetRenderTarget(cmd, cameraData.renderer.cameraColorTargetHandle, cameraData.renderer.cameraDepthTargetHandle);
                else
                    CoreUtils.SetRenderTarget(cmd, cameraData.renderer.cameraColorTargetHandle);

                ExecuteMainPass(rasterCmd, kCopyActiveColor ? m_CopiedColor : null, m_Material);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            filteringSettings = new FilteringSettings(RenderQueueRange.all, layersToExclude);

            var drawSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, cameraData.defaultOpaqueSortFlags);
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);


            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        public void Dispose()
        {
            m_CopiedColor?.Release();
        }

        #endregion

        #region PASS_RENDER_GRAPH_PATH

        private class CopyPassData
        {
            public TextureHandle inputTexture;
        }

        private class MainPassData
        {
            public Material material;
            public TextureHandle inputTexture;
            public RendererListHandle rendererListHandle;
        }

        private static void ExecuteCopyColorPass(CopyPassData data, RasterGraphContext context)
        {
            ExecuteCopyColorPass(context.cmd, data.inputTexture);
        }

        private static void ExecuteMainPass(MainPassData data, RasterGraphContext context)
        {
            ExecuteMainPass(context.cmd, data.inputTexture.IsValid() ? data.inputTexture : null, data.material);
            context.cmd.DrawRendererList(data.rendererListHandle);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            filteringSettings = new FilteringSettings(RenderQueueRange.all, layersToExclude);

            UniversalRenderer renderer = (UniversalRenderer) cameraData.renderer;
            var colorCopyDescriptor = GetCopyPassTextureDescriptor(cameraData.cameraTargetDescriptor);
            TextureHandle copiedColor = TextureHandle.nullHandle;

            if (kCopyActiveColor)
            {
                copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_DeNorSobOutlineColorCopy", false);

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("DeNorSob Outline", out var passData, profilingSampler))
                {
                    passData.inputTexture = resourcesData.activeColorTexture;
                    builder.UseTexture(resourcesData.activeColorTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyColorPass(data, context));
                }
            }

            using (var builder = renderGraph.AddRasterRenderPass<MainPassData>("DeNorSob Outline", out var passData, profilingSampler))
            {
                passData.material = m_Material;

                if (kCopyActiveColor)
                {
                    passData.inputTexture = copiedColor;
                    builder.UseTexture(copiedColor, AccessFlags.Read);
                }

                builder.SetRenderAttachment(resourcesData.activeColorTexture, 0, AccessFlags.Write);

                if(kBindDepthStencilAttachment)
                    builder.SetRenderAttachmentDepth(resourcesData.activeDepthTexture, AccessFlags.Write);

                SortingCriteria sortFlags = cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shaderTagIdList, renderingData, cameraData, lightData, sortFlags);
                passData.rendererListHandle = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawSettings, filteringSettings));
                builder.UseRendererList(passData.rendererListHandle);

                builder.SetRenderFunc((MainPassData data, RasterGraphContext context) => ExecuteMainPass(data, context));
            }
        }

        #endregion
    }
}