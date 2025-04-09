using UnityEngine;
using static PingPongBuffer;

public class CubemapRenderer
{
    public readonly RenderTexture fullResRenderBuffer, downsampledRenderBuffer;

    private RendererConfig rendererConfig;

    const int CUBEMAP_FACE_COUNT = 6;

    public struct RendererConfig
    {
        public Resolution fullResolution, downsampledResolution;
    }

    public enum DispatchTargetMode
    {
        FullRes, Downsampled, Both
    }

    private enum InputBufferType
    {
        ColorBuffer, DepthBuffer
    }

    public CubemapRenderer(RendererConfig rendererConfig, bool createDownsampledBuffer = true)
    {
        this.rendererConfig = rendererConfig;

        fullResRenderBuffer = new RenderTexture(rendererConfig.fullResolution.width, rendererConfig.fullResolution.height, 0, RenderTextureFormat.RInt);
        fullResRenderBuffer.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        fullResRenderBuffer.volumeDepth = CUBEMAP_FACE_COUNT;
        fullResRenderBuffer.enableRandomWrite = true;
        fullResRenderBuffer.Create();

        if (!createDownsampledBuffer) return;

        downsampledRenderBuffer = new RenderTexture(rendererConfig.downsampledResolution.width, rendererConfig.downsampledResolution.height, 0, RenderTextureFormat.RInt);
        downsampledRenderBuffer.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        downsampledRenderBuffer.volumeDepth = CUBEMAP_FACE_COUNT;
        downsampledRenderBuffer.enableRandomWrite = true;
        downsampledRenderBuffer.Create();
        // downsampledRenderBuffer = new PingPongBuffer(rendererConfig.downsampledResolution, CUBEMAP_FACE_COUNT, RenderTextureFormat.RInt);
    }

    private void ClearRenderTexture(RenderTexture renderTexture, ClearMode clearMode)
    {
        if (clearMode == ClearMode.None)
        {
            return;
        }

        RenderTexture rt = RenderTexture.active;
        for (int faceIndex = 0; faceIndex < renderTexture.volumeDepth; faceIndex++)
        {
            Graphics.SetRenderTarget(renderTexture, 0, CubemapFace.Unknown, faceIndex);

            switch (clearMode)
            {
                case ClearMode.ColorBuffer:
                    GL.Clear(true, true, Color.clear);
                    break;
                case ClearMode.DepthBuffer:
                    // a bit odd, but InterlockedMin needs a high value as a starting point to find smallest depth
                    GL.Clear(true, true, new Color(int.MaxValue, 0, 0));
                    break;
                default:
                    Debug.LogError("ClearRenderTexture called with invalid clear mode.");
                    break;
            }
        }
        RenderTexture.active = rt;
    }

    public void Clear(bool clearFullRes = true, bool clearDownsampled = true)
    {
        if (clearFullRes)
        {
            ClearRenderTexture(fullResRenderBuffer, ClearMode.DepthBuffer);
        }

        if (clearDownsampled)
        {
            ClearRenderTexture(downsampledRenderBuffer, ClearMode.DepthBuffer);
        }
    }

    public void Render(RenderTexture source, Vector3 offset, Resolution dispatchResolution, int inputSliceOffset = 0, bool clearBuffer = true, DispatchTargetMode dispatchTargetMode = DispatchTargetMode.Both, RenderTexture depthMask = null)
    {
        if (dispatchTargetMode != DispatchTargetMode.FullRes && downsampledRenderBuffer == null)
        {
            Debug.LogError("Downsampled render buffer is not initialized. Cannot render to downsampled buffer.");
            return;
        }

        // determine input buffer type
        InputBufferType inputBufferType;
        switch (source.format)
        {
            case RenderTextureFormat.ARGBHalf:
                inputBufferType = InputBufferType.ColorBuffer;
                break;
            case RenderTextureFormat.RInt:
                inputBufferType = InputBufferType.DepthBuffer;
                break;
            default:
                Debug.LogError("Cubemap render call failed. Unsupported source format: " + source.format);
                return;
        }

        // convert render config to shader compatible types
        Vector2 inputResolution = new Vector2(source.width, source.height);
        Vector2 _dispatchResolution = new Vector2(dispatchResolution.width, dispatchResolution.height);
        Vector2 outputResolutionFull = new Vector2(rendererConfig.fullResolution.width, rendererConfig.fullResolution.height);
        Vector2 outputResolutionDownsampled = new Vector2(rendererConfig.downsampledResolution.width, rendererConfig.downsampledResolution.height);

        // set shader data
        RenderManager.CubemapComputeShaderData shaderData = RenderManager.cubemapComputeShaderData;
        ComputeShader computeShader = shaderData.computeShader;
        computeShader.SetBool("_UseDepthMask", depthMask != null);
        computeShader.SetInt("_InputType", (int)inputBufferType);
        computeShader.SetInt("_DispatchTargetMode", (int)dispatchTargetMode);
        computeShader.SetInt("_InputSliceOffset", inputSliceOffset);
        computeShader.SetFloat("_CamFarClip", Camera.main.farClipPlane);
        computeShader.SetVector("_Offset", offset);
        computeShader.SetVector("_InputResolution", inputResolution);
        computeShader.SetVector("_DispatchResolution", _dispatchResolution);
        computeShader.SetVector("_OutputResolutionFull", outputResolutionFull);
        computeShader.SetVector("_OutputResolutionDownsampled", outputResolutionDownsampled);

        // set inputs (we have to set them all in any case or the cs freaks out)
        computeShader.SetTexture(shaderData.renderKernelId, "_InputColorBuffer", source);
        computeShader.SetTexture(shaderData.renderKernelId, "_InputDepthBuffer", source);
        computeShader.SetTexture(shaderData.renderKernelId, "_DepthMask", depthMask == null ? source : depthMask);

        // set render targets
        ClearMode clearMode = clearBuffer ? ClearMode.DepthBuffer : ClearMode.None;
        ClearRenderTexture(fullResRenderBuffer, clearMode);
        ClearRenderTexture(downsampledRenderBuffer, clearMode);
        computeShader.SetTexture(shaderData.renderKernelId, "RW_OutputDepthBufferFull", fullResRenderBuffer);
        computeShader.SetTexture(shaderData.renderKernelId, "RW_OutputDepthBufferDownsampled", downsampledRenderBuffer);

        // dispatch compute shader
        int threadGroupsX = Mathf.CeilToInt(dispatchResolution.width / (int)shaderData.renderThreadsX);
        int threadGroupsY = Mathf.CeilToInt(dispatchResolution.height / (int)shaderData.renderThreadsY);
        computeShader.Dispatch(shaderData.renderKernelId, threadGroupsX, threadGroupsY, CUBEMAP_FACE_COUNT);
    }
}
