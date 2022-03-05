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

        public void Pause()
        {
            Task.Run(() =>
            {
                Wait();
                playing = false;
            });
        }

        private void DecodingThread()
        {
            playing = true;
            var s = new Stopwatch();

            while (playing)
            {
                if (seekRequest != -1)
                {
                    s.Restart();
                    seekFrame(seekRequest, threadIdx);
                    
                    s.Stop();
                    Debug.Log($"Seeking to {seekRequest} took {s.ElapsedMilliseconds}ms");
                    seekRequest = -1;
                }
                else
                {
                    s.Restart();
                    readFrame(Frame +1, threadIdx);
                    s.Stop();
                    Debug.Log($"Reading next frame took {s.ElapsedMilliseconds}ms");
                }
            }
        }

        private bool Wait(int workerThreadTimeout = 10000)
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
