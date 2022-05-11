using System.Diagnostics;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace PreRendering
{
    public partial class Decoder
    {
        public long Frame
        {
            get => currentFrame(threadIdx);
            set => seekFrame(value, threadIdx);
        }

        private int threadIdx;
        private bool playing = true;
        private long seekRequest = -1;
        private Task workerThread;

        public Decoder(int threadIdx, bool startPlaying = true)
        {
            this.threadIdx = threadIdx;
            if (startPlaying) Play();
        }

        public void Play()
        {
            if (playing) return;
            workerThread = Task.Run(DecodingThread);
        }

        public void Pause() => Task.Run(Wait);

        /// <summary>
        /// Synchronously decode a single frame
        /// </summary>
        public bool Decode(long frameIdx)
        {
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            bool success = seekFrame(frameIdx, threadIdx);
            stopwatch.Stop();

            if (!success)
            {
                Debug.LogError($"Failed to seek to frame {frameIdx}");
                return false;
            }
            Debug.Log($"Seeking to {frameIdx} took {stopwatch.ElapsedMilliseconds}ms");

            stopwatch.Restart();
            success = readFrame(frameIdx, threadIdx);
            stopwatch.Stop();

            if (!success)
            {
                Debug.LogError($"Failed to seek to frame {frameIdx}");
                return false;
            }
            Debug.Log($"Reading frame {frameIdx} took {stopwatch.ElapsedMilliseconds}ms");

            return true;
        }

        /// <summary>
        /// Synchronously decode a single image
        /// </summary>
        public bool Decode(string path)
        {
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            bool success = readImage(path, threadIdx);
            stopwatch.Stop();

            if (!success)
            {
                Debug.LogError($"Failed to read image at {path}");
                return false;
            }
            Debug.Log($"Reading image at {path} took {stopwatch.ElapsedMilliseconds}ms");



            return true;
        }

        private bool Wait()
        {
            playing = false;
            workerThread?.Wait(workerThreadTimeout);

            if (workerThread?.Status == TaskStatus.Running)
            {
                Debug.LogWarning($"Worker thread {threadIdx} not responding (waited {workerThreadTimeout}ms).");
                return false;
            }

            return true;
        }
    }
}
