using UnityEngine;

namespace PreRendering
{
    public static partial class ChunkIndexing
    {
        public struct ChunkPosition
        {
            public int channelBlock;
            Vector2Int value;

            public static implicit operator Vector2Int(ChunkPosition i) => i.value;

            /// <summary>
            /// The unique index of this chunk, propagating in rows from bottom left
            /// </summary>
            public ChunkIndex Global => new ChunkIndex(value, channelBlock);

            public ChunkPosition(Vector2 position, int channelBlock)
            {
                value = new Vector2Int(
                    Mathf.FloorToInt(position.x / config.chunkWidth),
                    Mathf.FloorToInt(position.y / config.chunkWidth));
                this.channelBlock = channelBlock;
            }
        }
    }
}