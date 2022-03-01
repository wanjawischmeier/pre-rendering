using UnityEngine;

namespace PreRendering
{
    public static partial class ChunkIndexing
    {
        public struct LocalPosition
        {
            Vector2Int value;

            public static implicit operator Vector2Int(LocalPosition i) => i.value;

            /// <summary>
            /// The index of this position relative to the parent chunk (repetitive)
            /// </summary>
            public LocalIndex Local => new LocalIndex(value);

            public LocalPosition(Vector2Int position)
            {
                value = new Vector2Int(
                    position.x % config.chunkWidth,
                    position.y % config.chunkWidth);
            }

            public LocalPosition(int localIndex)
            {
                int x = localIndex % config.chunkWidth;
                int y = Mathf.FloorToInt((localIndex - x) / (float)config.chunkWidth);

                value = new Vector2Int(x, y);
            }
        }
    }
}