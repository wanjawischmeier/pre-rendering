#include "pch.h"
#include "decoder.h"


bool InitializeBuffer(
    char* videoPath, int width, int height, int threads,
    FrameCallback frameCallback, ErrorMessage errorCallback,
    VideoInfo& info, int* error, uchar* buffer)
{
    instances = threads;
    frame_ready = frameCallback;
    error_callback = errorCallback;

    pCaps = new VideoCapture[instances];
    pMats = new Mat[instances];

    VideoCapture cap;

    try
    {
        cap = VideoCapture(videoPath);
        if (!cap.isOpened())
            CV_Error(Error::StsAssert, "Failed to open Video Capture");
    }
    catch (const Exception& ex)
    {
        errorCallback("Error while opening Video Capture", ex.what());
        return false;
    }

    for (size_t i = 0; i < instances; i++)
        pCaps[i] = VideoCapture(cap);

    info.width = (int)cap.get(CAP_PROP_FRAME_WIDTH);
    info.height = (int)cap.get(CAP_PROP_FRAME_HEIGHT);
    info.fps = (int)cap.get(CAP_PROP_FPS);
    info.frame_count = (size_t)cap.get(CAP_PROP_FRAME_COUNT);
    video_info = info;

    if (!(width == info.width && height == info.height))
    {
        image_resolution = Size(width, height);
        resize_image = true;
    }

    image_size = width * height * 3;
    pBuffer = new uchar[image_size * instances];
    buffer = pBuffer;

    return true;
}

DECODER bool ReadToBuffer(size_t frameIdx, int threadIdx, int bufferIdx)
{
    if (frameIdx >= video_info.frame_count)
    {
        error_callback("Frame index out of bounds", "");
        return false;
    }

    if (threadIdx >= instances)
    {
        error_callback("Thread index out of bounds", "");
        return false;
    }

    VideoCapture cap = pCaps[threadIdx];
    Mat mat = pMats[threadIdx];

    size_t currentFrame = (size_t)cap.get(CAP_PROP_POS_FRAMES);
    if (currentFrame != frameIdx)
        cap.set(CAP_PROP_POS_FRAMES, (double)frameIdx);

    if (!cap.read(mat))
    {
        error_callback("Failed to grab frame", "");
        return false;
    }


    if (resize_image)
    {
        try
        {
            resize(mat, mat, image_resolution);
        }
        catch (const Exception& ex)
        {
            error_callback("Failed to resize image", ex.what());
            return false;
        }
    }

    size_t start_idx = bufferIdx * image_size;
    size_t count = image_size * sizeof(uchar);
    memcpy(&pBuffer[start_idx], mat.data, count);
    frame_ready(frameIdx, threadIdx, bufferIdx);

    // thread decoding_thread(DecodeFrame, frameIdx, threadIdx, bufferIdx);
    return true;
}

DECODER void ReleaseBuffer()
{
    delete[] pCaps;
    delete[] pMats;
    delete pBuffer;
}

DECODER void DecodeFrame(size_t frameIdx, int threadIdx, int bufferIdx)
{
    /*
    VideoCapture cap = pCaps[threadIdx];
    Mat mat = pMats[threadIdx];

    size_t currentFrame = (size_t)cap.get(CAP_PROP_POS_FRAMES);
    if (currentFrame != frameIdx)
        cap.set(CAP_PROP_POS_FRAMES, (double)frameIdx);

    if (!cap.read(mat))
    {
        error_callback("Failed to grab frame", "");
        return;
    }


    if (resize_image)
    {
        try
        {
            resize(mat, mat, image_resolution);
        }
        catch (const Exception& ex)
        {
            error_callback("Failed to resize image", ex.what());
            return;
        }
    }

    size_t start_idx = bufferIdx * image_size;
    size_t count = image_size * sizeof(uchar);
    memcpy(&pBuffer[start_idx], mat.data, count);*/
    // frame_ready(frameIdx, threadIdx, bufferIdx);
}
