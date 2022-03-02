using System.Threading;
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
        private Task workerThread;

        public Decoder(int threadIdx, bool startPlaying = true)
        {
            this.threadIdx = threadIdx;
            if (startPlaying) Play();
        }

        /// <summary>
        /// Adds a frame to the decoding queue
        /// </summary>
        /// <param name="frameIdx">The frame to be added</param>
        /// <param name="threadIdx">On which thread it should be decoded</param>
        public void Decode(long frameIdx)
        {
            pendingFrames.Add(new DecodingFrame()
            {
                frameIdx = frameIdx,
                threadIdx = threadIdx
            });
        }

        public bool Wait(int workerThreadTimeout = 10000)
        {
            workerThread.Wait(workerThreadTimeout);

            if (workerThread.Status == TaskStatus.Running)
            {
                Debug.LogWarning($"Worker thread {threadIdx} not responding (waited {workerThreadTimeout}ms).");
                return false;
            }

            return true;
        }

        public void Play() => workerThread = Task.Run(Worker);

        public void Pause() => playing = false;

        private void Worker()
        {
            playing = true;

            var s = new Stopwatch();

            while (playing)
            {
                if (pendingFrames.Count > 0)
                {
                    DecodingFrame frame = pendingFrames[0];
                    pendingFrames.RemoveAt(0);
                    s.Restart();
                    readFrame(frame.frameIdx, frame.threadIdx);
                }
                else if (reading)
                {
                    reading = false;
                    s.Stop();
                    Debug.Log($"Reading frame took {s.ElapsedMilliseconds}ms");
                }
                else Thread.Sleep(100);
            }
        }
    }
}
