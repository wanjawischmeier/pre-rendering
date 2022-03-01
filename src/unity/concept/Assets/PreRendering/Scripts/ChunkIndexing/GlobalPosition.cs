using UnityEngine;

namespace PreRendering
{
    public static partial class ChunkIndexing
    {
        public struct GlobalPosition
        {
            public int channelBlock;
            Vector2Int value;

            public static implicit operator Vector2Int(GlobalPosition i) => i.value;

            /// <summary>
            /// The index of this grid position, propagating in rows from the bottom left
            /// </summary>
            public GlobalIndex Global => new GlobalIndex(value, channelBlock);

            /// <summary>
            /// The position relative to the parent chunk's location (repetitive)
            /// </summary>
            public LocalPosition Local => new LocalPosition(value);

            public GlobalPosition(Vector2 position, int channelBlock)
            {
                value = new Vector2Int(
                    Mathf.FloorToInt(position.x / config.blockWidth),
                    Mathf.FloorToInt(position.y / config.blockHeight));
                this.channelBlock = channelBlock;
            }

            public GlobalPosition(GlobalIndex globalIndex)
            {
                var localPosition = globalIndex.Local.Local;
                var chunkIndex = globalIndex.Chunk.Global;

                int x = (int)(chunkIndex % chunkSize);
                int y = Mathf.FloorToInt((0 - x) / (float)config.chunkWidth);


                value = new Vector2Int(x, y);
                channelBlock = globalIndex.channelBlock;
            }
        }
    }
}