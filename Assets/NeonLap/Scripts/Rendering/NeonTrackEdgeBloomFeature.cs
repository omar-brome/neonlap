using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace NeonLap.Rendering
{
    public class NeonTrackEdgeBloomFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            [Range(0f, 1.5f)] public float intensity = 0.38f;
        }

        class NeonTrackEdgeBloomPass : ScriptableRenderPass
        {
            readonly Settings settings;
            Material material;

            class PassData
            {
                internal TextureHandle source;
                internal TextureHandle destination;
                internal Material material;
                internal float intensity;
                internal float pulse;
            }

            public NeonTrackEdgeBloomPass(Settings featureSettings)
            {
                settings = featureSettings;
                renderPassEvent = featureSettings.passEvent;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null)
                {
                    var shader = Shader.Find("Hidden/NeonLap/NeonTrackEdgeBloom");
                    if (shader == null)
                        return;

                    material = CoreUtils.CreateEngineMaterial(shader);
                }

                var resourceData = frameData.Get<UniversalResourceData>();
                var source = resourceData.cameraColor;
                if (!source.IsValid())
                    return;

                var cameraData = frameData.Get<UniversalCameraData>();
                var desc = cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                var destination = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,
                    desc,
                    "_NeonLapEdgeBloomTemp",
                    false,
                    FilterMode.Bilinear);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                           passName,
                           out var passData,
                           profilingSampler))
                {
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    passData.source = source;
                    builder.UseTexture(source, AccessFlags.Read);
                    passData.destination = destination;
                    passData.material = material;
                    passData.intensity = settings.intensity;
                    passData.pulse = NeonTrackEdgePulseDriver.GlobalPulse;

                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                    {
                        data.material.SetFloat("_NeonPulse", data.pulse);
                        data.material.SetFloat("_Intensity", data.intensity);

                        Blitter.BlitTexture(context.cmd, data.source, Vector2.one, data.material, 0);
                    });
                }

                resourceData.cameraColor = destination;
            }

            public void Dispose()
            {
                CoreUtils.Destroy(material);
            }
        }

        public Settings settings = new();
        NeonTrackEdgeBloomPass pass;

        public override void Create()
        {
            pass = new NeonTrackEdgeBloomPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pass == null || renderingData.cameraData.cameraType != CameraType.Game)
                return;

            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            pass?.Dispose();
        }
    }
}
