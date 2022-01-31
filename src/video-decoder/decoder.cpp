#include "pch.h"
#include "decoder.h"

/*
bool InitializeBuffer(
    char* videoPath, FrameReady frameReady,
    int width, int height, int depth,
    VideoInfo* info, ushort* buffer)
{
    /*
    try
    {
        color = VideoCapture(videoPath);
        maps = VideoCapture(videoPath);
    }
    catch (const Exception&)
    {
        return false;
    }
    
    info = new VideoInfo();
    info->width = (int)color.get(CAP_PROP_FRAME_WIDTH);
    info->height = (int)color.get(CAP_PROP_FRAME_HEIGHT);
    info->fps = (int)color.get(CAP_PROP_FPS);
    info->frame_count = (size_t)color.get(CAP_PROP_FRAME_COUNT);

    maps_offset = info->frame_count/2;
    
    color_mat = Mat();
    maps_mat = Mat();

    if (!(width == info->width && height == info->height))
    {
        image_resolution = Size(width, height);
        resize_image = true;
    }

    image_size = width * height * 3;
    pBuffer = new ushort[image_size * 2];
    buffer = pBuffer;
    
    frame_ready = frameReady;

    Sleep(4000);
    frame_ready(32);

    return true;
}

bool ReadToBuffer(size_t frame)
{
    if (frame >= maps_offset)
        return false;

    size_t maps_frame = frame + maps_offset;

    color.set(CAP_PROP_POS_FRAMES, (double)frame);
    maps.set(CAP_PROP_POS_FRAMES, (double)frame + maps_offset);
    
    if (!(color.read(color_mat) && maps.read(maps_mat)))
        return false;

    if (resize_image)
    {
        resize(color_mat, color_mat, image_resolution);
        resize(maps_mat, maps_mat, image_resolution);
    }

    size_t color_start = frame * image_size;
    size_t maps_start = maps_frame * image_size;

    memcpy(&pBuffer[color_start], color_mat.data, image_size * sizeof(ushort));
    memcpy(&pBuffer[maps_start], maps_mat.data, image_size * sizeof(ushort));

    frame_ready(frame);

    return true;
}
*/
DECODER void ReleaseBuffer()
{
    for (size_t i = 0; i < instances; i++)
    {
        VideoCapture cap = caps[i];
        if (cap.isOpened())
            cap.release();

        mats[i].release();
    }

    if (pBuffer == nullptr)
        return;

    delete pBuffer;
    pBuffer = nullptr;
}

DECODER void TestCallback(FrameReady callback)
{
    callback(24);
    *out_error_code = 64;
}

DECODER bool TestInit(char* videoPath, int threads, VideoInfo& info, int* error)
{
    instances = threads;
    out_error_code = error;

    caps = new VideoCapture[instances];
    mats = new Mat[instances];

    try
    {
        caps[0] = VideoCapture(videoPath);

        for (size_t i = 1; i < instances; i++)
            caps[i] = VideoCapture(caps[0]);
    }
    catch (const Exception&)
    {
        *out_error_code = 1;
        return false;
    }

    VideoCapture cap = caps[0];
    info.width = (int)cap.get(CAP_PROP_FRAME_WIDTH);
    info.height = (int)cap.get(CAP_PROP_FRAME_HEIGHT);
    info.fps = (int)cap.get(CAP_PROP_FPS);
    info.frame_count = (size_t)cap.get(CAP_PROP_FRAME_COUNT);

    return true;
}