using UnityEngine;

public class MinimapShaderController : MonoBehaviour
{
    public Transform player;
    public Material minimapMaterial;
    public RenderManager renderManager;

    private Vector4[] markedCells = new Vector4[10]; // Reduced to 10 marked cells

    private void Start()
    {
        Vector4[] cubemapPositions = renderManager.map.cubemapPositions;
        for (int i = 0; i < cubemapPositions.Length; i++)
        {
            markedCells[i] = cubemapPositions[i];
        }
    }

    private void Update()
    {
        // Move grid based on player's position (XZ plane)
        Vector3 playerPos = player.position;
        minimapMaterial.SetVector("_PlayerPosition", new Vector4(playerPos.x, playerPos.z, 0, 0));

        // Get player's forward direction (normalized to 2D)
        Vector3 forward = player.forward;
        Vector2 playerDir = new Vector2(forward.x, forward.z);
        minimapMaterial.SetVector("_PlayerDirection", new Vector4(playerDir.x, playerDir.y, 0, 0));
        minimapMaterial.SetVectorArray("MarkedCells", markedCells);

        Vector4[] cachedPositions = new Vector4[100];
        var buffer = renderManager.circularBuffer.RawBuffer;
        for (int i = 0; i < buffer.Length; i++)
        {
            var frame = buffer[i];
            if (!frame.isProjected) continue;

            cachedPositions[i] = new Vector4(frame.position.x, frame.position.y, frame.position.z, 1);
        }
        minimapMaterial.SetVectorArray("_CachedPositions", cachedPositions);
    }
}
