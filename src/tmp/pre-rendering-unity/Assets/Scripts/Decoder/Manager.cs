using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;

/// <summary>
/// Namespace for the dynamic PreRendering and buffering of video frames using the VideoPlayer class
/// </summary>
namespace PreRendering
{
    public class Manager
    {
        /// <summary>
        /// The frames that need to be decoded
        /// </summary>
        public static List<long> toDecode;
        readonly object toDecodeLock = new object();
        /// <summary>
        /// The frames that are currently being decoded
        /// </summary>
        public static List<long> pending;
        /// <summary>
        /// The threads that are currently available for decoding
        /// </summary>
        public static List<DecodingThread> availabe;
        /// <summary>
        /// Whether the mainloop is running
        /// </summary>
        public bool isDecoding
        {
            get
            {
                return (mainLoop.ThreadState == ThreadState.Running);
            }

            set
            {
                if (value) mainLoop.Start();
                else mainLoop.Interrupt();
            }
        }
        /// <summary>
        /// The player's position, should be set in the Update/FixedUpdate loop from the MonoBehaviour this cass instance was created in
        /// </summary>
        public Vector3 position;
        public long iters;
        Thread mainLoop;
        // Thread neededFrames;
        int mapSize;
        int bufferRadius;
        int bufferSize;

        /// <summary>
        /// Creates a new instance of the Manager class, needs the mapSize of the videoClip being used
        /// </summary>
        public Manager(int mapSize, int bufferRadius, int bufferSize, int threads, VideoClip map)
        {
            this.mapSize = mapSize;
            this.bufferRadius = bufferRadius;
            this.bufferSize = bufferSize;
            
            toDecode = new List<long>();
            pending = new List<long>();
            availabe = new List<DecodingThread>();

            for (int i = 0; i < threads; i++) new DecodingThread(map, i);   // Initialize threads for decoding

            mainLoop = new Thread(new ThreadStart(MainLoop));               // Start the mainloop on a new thread and store it
            // neededFrames = new Thread(new ThreadStart(SetNeededFrames));

            mainLoop.Priority = System.Threading.ThreadPriority.Lowest;
            mainLoop.IsBackground = false;

            mainLoop.Start();
            // neededFrames.Start();
        }

        void MainLoop()
        {
            while (true)
            {
                toDecode.Clear();

                for (int w = 0; w < bufferRadius; w++)
                {
                    for (int i = -w; i < w; i++)
                    {
                        toDecode.Add(Mathf.RoundToInt((position.x + i) + (position.y + w) * mapSize));
                        toDecode.Add(Mathf.RoundToInt((position.x - i) + (position.y - w) * mapSize));
                        toDecode.Add(Mathf.RoundToInt((position.x + w) + (position.y - i) * mapSize));
                        toDecode.Add(Mathf.RoundToInt((position.x - w) + (position.y + i) * mapSize));
                    }
                }
                foreach (long frame in toDecode)
                {
                    if (                                                    // Check if the frame is already being decoded
                        !pending.Contains(frame) && !FrameBuffer.Contains(frame) && 
                        availabe.Count != 0 && pending.Count < bufferSize
                    )
                    {
                        DecodingThread thread = availabe.ElementAt(0);      // Get the first available decoder

                        pending.Add(frame);                                 // Add the frame to the list of frames that are currently being decoded

                        Task.Run(() => thread.Decode(frame));               // Start decoding on a new thread
                    }
                }

                Thread.Sleep(10);
                iters++;
            }
        }

        void SetNeededFrames()
        {
            while (true)
            {
                toDecode.Clear();

                for (int w = 0; w < bufferRadius; w++)
                {
                    for (int i = -w; i < w; i++)
                    {
                        toDecode.Add(Mathf.RoundToInt((position.x + i) + (position.y + w) * mapSize));
                        toDecode.Add(Mathf.RoundToInt((position.x - i) + (position.y - w) * mapSize));
                        toDecode.Add(Mathf.RoundToInt((position.x + w) + (position.y - i) * mapSize));
                        toDecode.Add(Mathf.RoundToInt((position.x - w) + (position.y + i) * mapSize));
                    }
                }
            }
        }


        public static class Extensions
        {
            /// <summary>
            /// Static class for simple convertion between coordinates (x, y) and indices
            /// </summary>
            public static long CoordinatesToIndex(int x, int y, int width)
            {
                return (x + y * width);                                         // Apply simple formula
            }
        }
    }

    public static class Helper
    {
        /// <summary>
        /// Static class for simple convertion between coordinates (x, y) and indices
        /// </summary>
        public static long CoordinatesToIndex(int x, int y, int width)
        {
            return (x + y * width);                                         // Apply simple formula
        }

        public static Vector3Int FloorToInt(this Vector3 vector)
        {
            Vector3Int conv = new Vector3Int();

            conv.x = Mathf.FloorToInt(vector.x);
            conv.y = Mathf.FloorToInt(vector.y);
            conv.z = Mathf.FloorToInt(vector.z);

            return conv;
        }
    }
}