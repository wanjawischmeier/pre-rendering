using UnityEngine;

public class CubemapLoader : MonoBehaviour
{
    public Material cubemapApproximationMaterial;
    public Texture2D[] cubemapTextures;
    public int textureIndex;

    private void Start()
    {
        // apply material to all rendered meshes
        var meshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        foreach (var meshRenderer in meshRenderers)
        {
            meshRenderer.material = cubemapApproximationMaterial;
        }

        // load cubemap textures to gpu
        var sampleTexture = cubemapTextures[0];
        var cubemapTextureArray = new Texture2DArray(sampleTexture.width, sampleTexture.height, cubemapTextures.Length, sampleTexture.format, false);
        for (int textureIndex = 0; textureIndex < cubemapTextures.Length; textureIndex++)
        {
            Graphics.CopyTexture(cubemapTextures[textureIndex], 0, cubemapTextureArray, textureIndex);
        }

        cubemapApproximationMaterial.SetMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.alternateInverseOrientationMatricies);
        cubemapApproximationMaterial.SetTexture("_CubemapTextures", cubemapTextureArray);
    }

    private void Update()
    {
        cubemapApproximationMaterial.SetInteger("TEXTURE_INDEX", textureIndex);
    }

    private void OnDestroy()
    {
        
    }
}
