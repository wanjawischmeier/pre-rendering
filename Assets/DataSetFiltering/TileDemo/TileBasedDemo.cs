using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TileBasedDemo : MonoBehaviour
{
    [Serializable]
    public enum TileSize
    {
        Size4x4 = 4,
        Size8x8 = 8,
        Size16x16 = 16,
        Size32x32 = 32
    }

    private Dictionary<TileSize, string> tileSizeKeywords = new Dictionary<TileSize, string>()
    {
        { TileSize.Size4x4, "TILE_SIZE_4" },
        { TileSize.Size8x8, "TILE_SIZE_8" },
        { TileSize.Size16x16, "TILE_SIZE_16" },
        { TileSize.Size32x32, "TILE_SIZE_32" }
    };

    public Material material;          // Material, das den Shader verwendet
    public Texture2D texture;          // Die hochauflösende Textur
    public ComputeShader computeShader; // Der Compute Shader
    public TileSize tileSize = TileSize.Size8x8; // Die Größe der Tiles

    const int MAX_VALID_TEXELS = 4;      // Maximale Anzahl gültiger Texel pro Tile

    // Der Compute Shader Buffer für die Ergebnisse
    private ComputeBuffer tileBuffer;
    private TileSize previousTileSize = (TileSize)(-1);

    private void Start()
    {
        int bufferSizeWidth = texture.width / (int)tileSize; // Berechnung der Breite des Buffers  
        Debug.Log($"Buffer Size Width: {bufferSizeWidth}");

        // Texturgröße und Tile-Größe an das Material übergeben  
        material.SetInt("_HighResWidth", texture.width);
        material.SetInt("_LowResWidth", bufferSizeWidth);

        // TileBuffer anlegen, die Anzahl der Tiles ist 64x64  
        int numTiles = bufferSizeWidth * bufferSizeWidth;
        tileBuffer = new ComputeBuffer(numTiles, sizeof(float) * 2 * MAX_VALID_TEXELS + sizeof(int));

        // Shader-Parameter setzen  
        int kernelHandle = computeShader.FindKernel("GroupTexelsIntoTiles");

        // Setze alle Tile-Size-Keywords auf false  
        foreach (var keyword in tileSizeKeywords.Values)
        {
            computeShader.DisableKeyword(keyword);
        }

        // Setze das entsprechende Tile-Size-Keyword auf true  
        computeShader.EnableKeyword(tileSizeKeywords[tileSize]);

        computeShader.SetInt("_HighResWidth", texture.width);  // Die hochauflösende Textur an den Compute Shader übergeben  
        computeShader.SetInt("_LowResWidth", bufferSizeWidth);  // Die hochauflösende Textur an den Compute Shader übergeben  
        computeShader.SetTexture(kernelHandle, "_HighResTexture", texture);  // Die hochauflösende Textur an den Compute Shader übergeben  
        computeShader.SetBuffer(kernelHandle, "_TileBuffer", tileBuffer);    // Der Tile-Buffer für die Ergebnisse  

        // Shader Dispatch: 64x64 = 4096 Threads, jede Gruppe verarbeitet ein Tile  
        computeShader.Dispatch(kernelHandle, texture.width / (int)tileSize, texture.width / (int)tileSize, 1);

        // Den Tile-Buffer an das Material senden  
        material.SetInt("_TileBufferSize", bufferSizeWidth);
        material.SetBuffer("_TileBuffer", tileBuffer);
        material.SetTexture("_MainTex", texture);
    }

    private void Update()
    {
        if (previousTileSize == (TileSize)(-1))
        {
            // Wenn die Tile-Größe noch nicht gesetzt ist, die aktuelle Tile-Größe speichern
            previousTileSize = tileSize;
        }
        else if (previousTileSize != tileSize)
        {
            // Wenn die Tile-Größe geändert wurde, den Shader neu starten
            previousTileSize = tileSize;
            OnDestroy();
            Start();
        }
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
