using UnityEngine;

namespace PreRendering
{
    public static partial class ChunkIndexing
    {
        #region Data

        public static int chunkSize, totalSize, circleRadius;
        public static Vector2[] circularOffsets;
        public static int[] chunkIndicies;

        private static MapConfig config;

        #endregion

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

        /// <summary>
        /// Clamp the position vector to the grid bounds to avoid errors
        /// </summary>
        public static ClampedPosition ClampToChunkGrid(this Vector3 position, int channelBlock = 0) => new ClampedPosition(position.Flatten(), channelBlock);

        public static GlobalIndex GetGlobalIndex(this long frameIndex, out int channelBlock) => new GlobalIndex(frameIndex, out channelBlock);

        public static bool CorrectChunkIndex(GlobalIndex globalIndex, Vector3 position, out ChunkIndex newChunkIndex, out GlobalIndex newGlobalIndex)
        {
            newChunkIndex = position.ClampToChunkGrid(globalIndex.channelBlock).Chunk.Global;
            newGlobalIndex = newChunkIndex.Global;
            return globalIndex.Chunk == newChunkIndex;
        }
    }
}