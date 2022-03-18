using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace PreRendering
{
    public partial class Decoder
    {
        private void DecodingThread()
        {
            playing = true;
            bool success;
            var stopwatch = new Stopwatch();

            while (playing)
            {
                stopwatch.Restart();

                if (seekRequest != -1)
                {
                    success = seekFrame(seekRequest, threadIdx);
                    stopwatch.Stop();

                    if (!success)
                        Debug.LogError($"Failed to seek to frame {seekRequest}");
                    Debug.Log($"Seeking to {seekRequest} took {stopwatch.ElapsedMilliseconds}ms");

                    seekRequest = -1;
                    stopwatch.Restart();
                }

                success = readFrame(Frame + 1, threadIdx);
                stopwatch.Stop();

                if (!success)
                    Debug.LogError($"Failed to seek to frame {seekRequest}");
                Debug.Log($"Reading next frame took {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }
}