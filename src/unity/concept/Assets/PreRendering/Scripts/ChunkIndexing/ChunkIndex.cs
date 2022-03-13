using UnityEngine;

namespace PreRendering
{
    public static partial class ChunkIndexing
    {
        public struct ChunkIndex
        {
            public int channelBlock;
            int value;

            public static implicit operator int(ChunkIndex i) => i.value;

            /// <summary>
            /// This index represented as the index of a position on the grid
            /// </summary>
            public GlobalIndex Global => new GlobalIndex(value, channelBlock);

            public ChunkIndex(Vector2Int position, int channelBlock)
            {
                value = position.x + position.y * config.chunkColumns;
                this.channelBlock = channelBlock;
            }

            public ChunkIndex(long globalIndex, int channelBlock)
            {
                value = Mathf.FloorToInt(globalIndex / chunkSize);
                this.channelBlock = channelBlock;
            }
        }
    }
}