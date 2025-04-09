using UnityEngine;

/// <summary>
/// A circular buffer to store projected frames.
/// </summary>
public class CircularFrameBuffer
{
    /// <summary>
    /// Represents a frame with a position and a texture.
    /// </summary>
    public struct ProjectedFrame
    {
        public Vector3 position;
        public RenderTexture texture;
        public bool isProjected;
    }

    private ProjectedFrame[] buffer;
    private int currentIndex;
    private int size;

    /// <summary>
    /// Initializes a new instance of the CircularBuffer class with the specified size.
    /// </summary>
    /// <param name="size">The size of the buffer.</param>
    public CircularFrameBuffer(int size, RenderTextureDescriptor renderTextureDescriptor)
    {
        this.size = size;
        buffer = new ProjectedFrame[size];
        currentIndex = -1;

        for (int i = 0; i < size; i++)
        {
            buffer[i] = new ProjectedFrame
            {
                position = Vector3.zero,
                texture = RenderTexture.GetTemporary(renderTextureDescriptor),
                isProjected = false
            };
        }
    }

    ~CircularFrameBuffer()
    {
        for (int i = 0; i < size; i++)
        {
            if (buffer[i].texture != null)
            {
                RenderTexture.ReleaseTemporary(buffer[i].texture);
                buffer[i].texture = null;
            }
        }
    }

    public int Size => size;

    public ProjectedFrame[] RawBuffer => buffer;

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
    public ProjectedFrame? CurrentFrame
    {
        get
        {
            if (currentIndex == -1) return null;
            return buffer[currentIndex];
        }
    }

    /// <summary>
    /// Gets the texture of the last frame in the buffer.
    /// </summary>
    public ProjectedFrame? PreviousFrame
    {
        get
        {
            if (currentIndex == -1) return null;
            return buffer[PreviousFrameIndex];
        }
    }

    /// <summary>
    /// Adds a new frame to the buffer. New frame will be held in <see cref="CurrentFrame"/>.
    /// </summary>
    /// <param name="frame">The frame to add.</param>
    public void Push(ProjectedFrame frame)
    {
        currentIndex = (currentIndex + 1) % size;

        Graphics.CopyTexture(frame.texture, buffer[currentIndex].texture);
        buffer[currentIndex].isProjected = frame.isProjected;
        buffer[currentIndex].position = frame.position;
    }

    /// <summary>
    /// Adds a new frame to the buffer. New frame will be held in <see cref="CurrentFrame"/>.
    /// </summary>
    public void Push(RenderTexture texture, Vector3 position)
    {
        Push(new ProjectedFrame
        {
            texture = texture,
            position = position,
            isProjected = true
        });
    }

    /// <summary>
    /// Gets the frame at the specified number of positions back.
    /// </summary>
    /// <param name="n">The number of positions back.</param>
    /// <returns>The frame at the specified number of positions back, or null if not available.</returns>
    public ProjectedFrame? GetFrame(int n)
    {
        if (currentIndex == -1 || n >= size) return null;
        int index = (currentIndex - n + size) % size;
        ProjectedFrame frame = buffer[index];

        if (!frame.isProjected) return null;
        return frame;
    }
}
