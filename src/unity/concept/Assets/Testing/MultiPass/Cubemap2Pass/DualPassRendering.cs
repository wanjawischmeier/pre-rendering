using UnityEngine;

public class DualPassRendering : MonoBehaviour
{
    public Camera mainCamera;
    public Camera secondaryCamera;
    public RenderTexture depthTexture;

    void Start()
    {
        if (mainCamera == null || secondaryCamera == null)
        {
            Debug.LogError("Cameras not assigned!");
            return;
        }

        // Set secondary camera to render after main camera
        secondaryCamera.depth = mainCamera.depth + 1;

        // Set up Render Texture for depth texture
        depthTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth);
        depthTexture.Create();
        mainCamera.targetTexture = depthTexture;

        // Set depth texture as a shader property for the secondary camera
        Shader.SetGlobalTexture("_MainDepthTexture", depthTexture);
    }

    void OnDestroy()
    {
        // Clean up Render Texture
        if (depthTexture != null)
            Destroy(depthTexture);
    }
}
