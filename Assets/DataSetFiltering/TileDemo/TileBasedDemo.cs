using UnityEngine;

public class TileBasedDemo : MonoBehaviour
{
    public Material material;          // Material, das den Shader verwendet
    public Texture2D texture;          // Die hochauflösende Textur
    public ComputeShader computeShader; // Der Compute Shader
    public int bufferSizeWidth = 64;    // Größe des Puffers (64x64 Tiles)

    const int TILE_SIZE = 16;            // Größe des Tiles (16x16)
    const int MAX_VALID_TEXELS = 4;      // Maximale Anzahl gültiger Texel pro Tile

    // Der Compute Shader Buffer für die Ergebnisse
    private ComputeBuffer tileBuffer;

    private void Start()
    {
        // Texturgröße und Tile-Größe an das Material übergeben
        material.SetInt("_HighResWidth", texture.width);
        material.SetInt("_LowResWidth", bufferSizeWidth);

        // TileBuffer anlegen, die Anzahl der Tiles ist 64x64
        int numTiles = bufferSizeWidth * bufferSizeWidth;
        tileBuffer = new ComputeBuffer(numTiles, sizeof(float) * 2 * MAX_VALID_TEXELS + sizeof(int));

        // Shader-Parameter setzen
        int kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.SetInt("_HighResWidth", texture.width);  // Die hochauflösende Textur an den Compute Shader übergeben
        computeShader.SetInt("_LowResWidth", bufferSizeWidth);  // Die hochauflösende Textur an den Compute Shader übergeben
        computeShader.SetTexture(kernelHandle, "_HighResTexture", texture);  // Die hochauflösende Textur an den Compute Shader übergeben
        computeShader.SetBuffer(kernelHandle, "_TileBuffer", tileBuffer);    // Der Tile-Buffer für die Ergebnisse

        // Shader Dispatch: 64x64 = 4096 Threads, jede Gruppe verarbeitet ein Tile
        computeShader.Dispatch(kernelHandle, texture.width / TILE_SIZE, texture.width / TILE_SIZE, 1);

        // Den Tile-Buffer an das Material senden
        material.SetInt("_TileBufferSize", bufferSizeWidth);
        material.SetBuffer("_TileBuffer", tileBuffer);
        material.SetTexture("_MainTex", texture);
    }

    private void OnDestroy()
    {
        // ComputeBuffer freigeben, wenn das Skript zerstört wird
        if (tileBuffer != null)
        {
            tileBuffer.Release();
        }
    }
}
