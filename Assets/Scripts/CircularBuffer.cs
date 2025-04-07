using UnityEngine;

/// <summary>
/// A circular buffer to store projected frames.
/// </summary>
public class CircularBuffer
{
    /// <summary>
    /// Represents a frame with a position and a texture.
    /// </summary>
    public struct ProjectedFrame
    {
        public Vector3 position;
        public RenderTexture texture;
    }

    private ProjectedFrame[] buffer;
    private int currentIndex;
    private int size;

    /// <summary>
    /// Initializes a new instance of the CircularBuffer class with the specified size.
    /// </summary>
    /// <param name="size">The size of the buffer.</param>
    public CircularBuffer(int size)
    {
        this.size = size;
        buffer = new ProjectedFrame[size];
        currentIndex = -1;
    }

    /// <summary>
    /// Gets the index of the current frame in the buffer.
    /// </summary>
    public int CurrentFrameIndex => currentIndex;

    /// <summary>
    /// Gets the index of the previous frame in the buffer.
    /// </summary>
    public int PreviousFrameIndex
    {
        get
        {
            if (currentIndex == -1) return -1;
            return (currentIndex - 1 + size) % size;
        }
    }

    /// <summary>
    /// Gets the texture of the current frame in the buffer.
    /// </summary>
    public RenderTexture CurrentFrame
    {
        get
        {
            if (currentIndex == -1) return null;
            return buffer[currentIndex].texture;
        }
    }

    /// <summary>
    /// Gets the texture of the last frame in the buffer.
    /// </summary>
    public RenderTexture PreviousFrame
    {
        get
        {
            if (currentIndex == -1) return null;
            return buffer[PreviousFrameIndex].texture;
        }
    }

    /// <summary>
    /// Adds a new frame to the buffer. New frame will be held in <see cref="CurrentFrame"/>.
    /// </summary>
    /// <param name="frame">The frame to add.</param>
    public void Push(ProjectedFrame frame)
    {
        currentIndex = (currentIndex + 1) % size;
        buffer[currentIndex] = frame;
    }
}
