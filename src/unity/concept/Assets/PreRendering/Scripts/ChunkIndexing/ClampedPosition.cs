using UnityEngine;

namespace PreRendering
{
    public static partial class ChunkIndexing
    {
        public struct ClampedPosition
        {
            public int channelBlock;
            Vector2 value;

            public static implicit operator Vector2(ClampedPosition i) => i.value;

            /// <summary>
            /// The corner point position of the parent chunk
            /// </summary>
            public ChunkPosition Chunk => new ChunkPosition(value, channelBlock);

            /// <summary>
            /// The position snapped to the corner of the closest block
            /// </summary>
            public GlobalPosition Grid => new GlobalPosition(value, channelBlock);

            public ClampedPosition(Vector2 position, int channelBlock)
            {
                value = new Vector2(
                    Mathf.Clamp(position.x, 0, config.chunkWidth * config.chunkColumns - 1),
                    Mathf.Clamp(position.y, 0, config.chunkWidth * config.chunkRows - 1));
                this.channelBlock = channelBlock;
            }
        }
    }
}