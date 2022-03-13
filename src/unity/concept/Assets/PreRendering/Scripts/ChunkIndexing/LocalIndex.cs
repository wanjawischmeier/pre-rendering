using UnityEngine;

namespace PreRendering
{
    public static partial class ChunkIndexing
    {
        public struct LocalIndex
        {
            int value;

            public static implicit operator int(LocalIndex i) => i.value;

            /// <summary>
            /// The position of this index relative to the parent chunk (repetitive)
            /// </summary>
            public LocalPosition Local => new LocalPosition(value);

            public LocalIndex(Vector2Int position)
            {
                value = position.x + position.y * config.chunkWidth;
            }

            public LocalIndex(long globalIndex)
            {
                value = (int)(globalIndex % chunkSize);
            }
        }
    }
}