using UnityEngine;

namespace PreRendering
{
    public static partial class ChunkIndexing
    {
        public struct GlobalIndex
        {
            public int channelBlock;
            long value;

            public static implicit operator long(GlobalIndex i) => i.value;

            /// <summary>
            /// The unique index of the parent chunk containing this index
            /// </summary>
            public ChunkIndex Chunk { get { return new ChunkIndex(value, channelBlock); } }

            /// <summary>
            /// The index of this index relative to the parent chunk (repetitive)
            /// </summary>
            public LocalIndex Local => new LocalIndex(value);

            public GlobalIndex(Vector2Int position, int channelBlock)
            {
                value = position.x + position.y * config.chunkWidth * config.chunkColumns;
                this.channelBlock = channelBlock;
            }

            public GlobalIndex(int chunkIndex, int channelBlock)
            {
                value = chunkIndex * chunkSize + channelBlock * totalSize;
                this.channelBlock = channelBlock;
            }

            public GlobalIndex(long frameIndex, out int channelBlock)
            {
                value = frameIndex % totalSize;
                this.channelBlock = channelBlock = (int)(frameIndex - value) / totalSize;
            }
        }
    }
}