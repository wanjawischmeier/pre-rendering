using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;

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
                return (mainLoop.Status == TaskStatus.WaitingToRun);
            }

            set
            {
                if (value) mainLoop.Start();
                else mainLoop.Wait();
            }
        }
        Transform player;
        Task mainLoop;
        int mapSize;
        int bufferRadius;

        /// <summary>
        /// Creates a new instance of the Manager class, needs the mapSize of the videoClip being used
        /// </summary>
        public Manager(int mapSize, int bufferRadius, Transform player, int threads, VideoClip map)
        {
            this.mapSize = mapSize;
            this.bufferRadius = bufferRadius;
            this.player = player;
            
            toDecode = new List<long>();
            pending = new List<long>();
            availabe = new List<DecodingThread>();

            for (int i = 0; i < threads; i++) new DecodingThread(map, i);   // Initialize threads for decoding
            
            mainLoop = Task.Run(() => MainLoop());                          // Start the mainloop on a new thread and store it
        }

        void MainLoop()
        {
            while (true)
            {
                SetNeededFrames(bufferRadius, mapSize, new Vector2(player.position.x, player.position.z), out toDecode);

                foreach (long frame in toDecode)
                {
                    if (!pending.Contains(frame))                           // Check if the frame is already being decoded
                    {
                        DecodingThread thread = availabe.ElementAt(0);      // Get the first available decoder

                        pending.Add(frame);                                 // Add the frame to the list of frames that are currently being decoded

                        Task.Run(() => thread.Decode(frame));               // Start decoding on a new thread
                    }
                }
            }
        }

        void SetNeededFrames(int searchRadius, int width, Vector2 position, out List<long> toDecode)
        {
            toDecode = new List<long>();
            
            for (int w = 0; w < searchRadius; w++)
            {
                for (int i = -w; i < w; i++)
                {
                    toDecode.Add(Mathf.RoundToInt((position.x + i) + (position.y + w) * width));
                    toDecode.Add(Mathf.RoundToInt((position.x - i) + (position.y - w) * width));
                    toDecode.Add(Mathf.RoundToInt((position.x + w) + (position.y - i) * width));
                    toDecode.Add(Mathf.RoundToInt((position.x - w) + (position.y + i) * width));
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