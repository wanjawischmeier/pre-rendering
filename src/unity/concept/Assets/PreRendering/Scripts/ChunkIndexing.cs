using UnityEngine;

namespace PreRendering
{
    public static class ChunkIndexing
    {
        public static int chunkSize, totalSize, circleRadius;
        public static Vector2[] circularOffsets;
        public static int[] chunkIndicies;

        private static MapConfig config;

        public static void CalculateConstants(MapConfig config, int searchCircleRadius)
        {
            ChunkIndexing.config = config;
            circleRadius = searchCircleRadius;

            chunkSize = Mathf.RoundToInt(Mathf.Pow(config.chunkWidth, 2));
            totalSize = chunkSize * config.chunkColumns * config.chunkRows;

            // Based on https://stackoverflow.com/a/31864777/13215204
            int x = 0;
            int y = 0;
            int numPoints = Mathf.RoundToInt(Mathf.Pow(2 * circleRadius - 1, 2));
            circularOffsets = new Vector2[numPoints];
            chunkIndicies = new int[chunkSize * config.channelBlocks];

            for (int i = 0; i < numPoints; ++i)
            {
                circularOffsets[i] = new Vector2(x, y);

                if (Mathf.Abs(x) <= Mathf.Abs(y) && (x != y || x >= 0))
                    x += ((y >= 0) ? 1 : -1);
                else
                    y += ((x >= 0) ? -1 : 1);
            }
        }

        public static Vector2 Flatten(this Vector3 vector) => new Vector2(vector.x, vector.z);

        public static Vector3 Expand(this Vector2 vector) => new Vector3(vector.x, 0, vector.y);

        public static bool CorrectChunkIndex(GlobalIndex globalIndex, Vector3 position, out ChunkIndex newChunkIndex, out GlobalIndex newGlobalIndex)
        {
            newChunkIndex = position.ClampToChunkGrid(globalIndex.channelBlock).Chunk.Global;
            newGlobalIndex = newChunkIndex.Global;
            return globalIndex.Chunk == newChunkIndex;
        }

        /// <summary>
        /// Clamp the position vector to the grid bounds to avoid errors
        /// </summary>
        public static ClampedPosition ClampToChunkGrid(this Vector3 position, int channelBlock = 0) => new ClampedPosition(position.Flatten(), channelBlock);

        public static GlobalIndex GetGlobalIndex(this long frameIndex, out int channelBlock) => new GlobalIndex(frameIndex, out channelBlock);


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