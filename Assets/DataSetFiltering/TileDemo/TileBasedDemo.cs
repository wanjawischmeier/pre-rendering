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

    [Serializable]
    public enum TileCapacity
    {
        x2 = 2,
        x4 = 4,
        x8 = 8,
        x16 = 16
    }

    private Dictionary<TileSize, string> tileSizeKeywords = new Dictionary<TileSize, string>()
    {
        { TileSize.Size4x4, "TILE_SIZE_4" },
        { TileSize.Size8x8, "TILE_SIZE_8" },
        { TileSize.Size16x16, "TILE_SIZE_16" },
        { TileSize.Size32x32, "TILE_SIZE_32" }
    };

    private Dictionary<TileCapacity, string> tileCapacityKeywords = new Dictionary<TileCapacity, string>()
    {
        { TileCapacity.x2, "TILE_CAPACITY_2" },
        { TileCapacity.x4, "TILE_CAPACITY_4" },
        { TileCapacity.x8, "TILE_CAPACITY_8" },
        { TileCapacity.x16, "TILE_CAPACITY_16" }
    };

    public Material material;          // Material, das den Shader verwendet
    public Texture2D texture;          // Die hochauflösende Textur
    public ComputeShader computeShader; // Der Compute Shader
    public TileSize tileSize = TileSize.Size8x8;
    public TileCapacity tileCapacity = TileCapacity.x4;

    // Der Compute Shader Buffer für die Ergebnisse
    private ComputeBuffer tileBuffer;
    private TileSize previousTileSize = (TileSize)(-1);
    private TileCapacity previousTileCapacity = (TileCapacity)(-1);

    private void Start()
    {
        int bufferSizeWidth = texture.width / (int)tileSize; // Berechnung der Breite des Buffers  
        Debug.Log($"Buffer Size Width: {bufferSizeWidth}, Tile Capacity: {(int)tileCapacity}");

        // Texturgröße und Tile-Größe an das Material übergeben  
        material.SetInt("_HighResWidth", texture.width);
        material.SetInt("_LowResWidth", bufferSizeWidth);

        // TileBuffer anlegen, die Anzahl der Tiles ist 64x64  
        int numTiles = bufferSizeWidth * bufferSizeWidth;
        tileBuffer = new ComputeBuffer(numTiles, sizeof(float) * 2 * (int)tileCapacity + sizeof(int));

        // Shader-Parameter setzen  
        int kernelHandle = computeShader.FindKernel("GroupTexelsIntoTiles");

        // Setze alle Keywords auf false
        foreach (var keyword in tileSizeKeywords.Values)
        {
            computeShader.DisableKeyword(keyword);
        }

        foreach (var keyword in tileCapacityKeywords.Values)
        {
            computeShader.DisableKeyword(keyword);
            material.DisableKeyword(keyword);
        }

        // Setze das entsprechende Tile-Size-Keyword auf true  
        computeShader.EnableKeyword(tileSizeKeywords[tileSize]);
        computeShader.EnableKeyword(tileCapacityKeywords[tileCapacity]);
        material.EnableKeyword(tileCapacityKeywords[tileCapacity]);

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
            previousTileCapacity = tileCapacity;
        }
        else if (previousTileSize != tileSize || previousTileCapacity != tileCapacity)
        {
            // Wenn die Tile-Größe geändert wurde, den Shader neu starten
            previousTileSize = tileSize;
            previousTileCapacity = tileCapacity;

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
