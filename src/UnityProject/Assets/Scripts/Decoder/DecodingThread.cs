using UnityEngine;
using UnityEngine.Video;

namespace PreRendering
{
    /// <summary>
    /// A class for the multithreaded decoding of video frames
    /// </summary>
    public class DecodingThread
    {
        GameObject gameObject;
        VideoPlayer decoder;

        public DecodingThread(VideoClip clip, int id)
        {
            gameObject = new GameObject("Decoder " + id.ToString());    // Create the gameObject
            decoder = gameObject.AddComponent<VideoPlayer>();           // Create a new VideoPlayer

            decoder.clip = clip;                                        // Set up the VideoPlayer
            decoder.isLooping = true;
            decoder.playbackSpeed = 0;
            decoder.renderMode = VideoRenderMode.APIOnly;
            decoder.audioOutputMode = VideoAudioOutputMode.None;

            Manager.availabe.Add(this);                                 // Add this DecodingThread to the list of available threads
        }

        /// <summary>
        /// Decode a frame from the video clip this instance was initialized with
        /// </summary>
        public void Decode(long frameIdx)
        {
            Manager.availabe.Remove(this);                              // Remove this thread from the list of available threads

            decoder.frame = frameIdx;                                   // Set the frame of the decoder to the parameter

            Manager.availabe.Add(this);                                 // Set this thread as available again

            FrameBuffer.Push(frameIdx, decoder.texture);                // Get and push the decoded texture
        }
    }
}