using PreRendering;
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ShaderManager : MonoBehaviour
{
    #region Data

    public Shader shader;
    public float geometryPercision = 0.75f;
    public ShaderDebugMode shaderDebugging = ShaderDebugMode.Disabled;
    public float depthOfField = 0;
    public float mistOffset = 1;
    public float mistFalloff = 0.1f;
    public Color mistColor = Color.white;
    public float pIcon = 0.05f;

    [NonSerialized]
    public VideoClipDecoder decoder;
    [NonSerialized]
    public Material material;

    private Camera mainCamera;

    public enum ShaderDebugMode
    {
        Disabled,
        TextureCoordinates,
        ProjectedCoordinates,
        Normals,
        DepthOfField,
        DepthBuffer
    }

    #endregion

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        decoder = GetComponent<VideoClipDecoder>();
        material = new Material(shader);

        material.SetInt("MX_IDX", decoder.cacheSize);
        material.SetFloat("PI", Mathf.PI);
        material.SetFloat("PI2", Mathf.PI * 2);
        material.SetFloat("NegativeInfinity", float.NegativeInfinity);
        material.SetFloat("PositiveInfinity", float.PositiveInfinity);
        material.SetFloat("NCLIP", decoder.config.nclip);
        material.SetFloat("FCLIP", decoder.config.fclip);
        // material.SetBuffer("ChunkIndicies", buffer);
        // material.SetBuffer("_InputBuffer", decoder.decodingManager.buffer.compute);
    }

    private void Update()
    {
        material.SetInt("DEBUG", (int)shaderDebugging);
        material.SetFloat("FOV", mainCamera.fieldOfView * Mathf.Deg2Rad);
        material.SetFloat("PLAYER_ICON", pIcon);
        material.SetFloat("DOF_INTENSITY", depthOfField);
        material.SetFloat("MIST_FALLOFF", mistFalloff);
        material.SetFloat("MIST_OFFSET", mistOffset);
        material.SetVector("MIST_COLOR", mistColor);
        material.SetVector("POSITION", transform.position);
        material.SetVector("ROTATION", transform.eulerAngles * Mathf.Deg2Rad);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        int imageIdx = 0;

        foreach (var offset in ChunkIndexing.circularOffsets)
        {
            var clamped = (transform.position + offset.Expand()).ClampToChunkGrid();
            var chunkIndex = clamped.Chunk.Global;
            var localIndex = clamped.Grid.Local.Local;

            // The point is in the buffer
            if (ChunkIndexing.chunkIndicies[localIndex] == chunkIndex)
            {
                imageIdx = localIndex;
                break;
            }
        }

        material.SetInt("IMG_IDX", imageIdx);
        Graphics.Blit(null, destination, material);
        // Graphics.Blit(chunk, destination, available, 0);
    }
}
