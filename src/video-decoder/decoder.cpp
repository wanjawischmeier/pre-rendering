#include "pch.h"
#include "decoder.h"


uchar* InitializeBuffer(
    char *videoPath, int threads,
    FrameCallback frameCallback, ErrorMessage errorCallback,
    VideoInfo &rInfo, uchar *buffer)
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
        error_callback("Error while opening Video Capture", ex.what());
        return nullptr;
    }

    rInfo.width = (int)cap.get(CAP_PROP_FRAME_WIDTH);
    rInfo.height = (int)cap.get(CAP_PROP_FRAME_HEIGHT);
    rInfo.fps = (int)cap.get(CAP_PROP_FPS);
    rInfo.frame_count = (size_t)cap.get(CAP_PROP_FRAME_COUNT);
    video_info = rInfo;

    for (size_t i = 0; i < instances; i++)
    {
        pCaps[i] = VideoCapture(cap);
        pMats[i] = Mat(video_info.width, video_info.height, CV_8UC3);
    }

    image_size = video_info.width * video_info.height * 3;
    pBuffer = new uchar[image_size * instances];
    buffer = pBuffer;
    error_callback(to_string(pMats[0].isContinuous()).c_str(), "");

    return pBuffer;
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

    size_t current_frame = (size_t)cap.get(CAP_PROP_POS_FRAMES);
    if (current_frame != frameIdx)
        cap.set(CAP_PROP_POS_FRAMES, (double)frameIdx);

    if (!cap.read(mat))
    {
        error_callback("Failed to grab frame", "");
        return false;
    }

    size_t start_idx = bufferIdx * image_size;
    size_t count = image_size * sizeof(uchar);
    memcpy(&pBuffer[start_idx], mat.data, count);
    pBuffer[1234] = 234;
    frame_ready(frameIdx, threadIdx, bufferIdx);
    return true;
}

DECODER void ReleaseBuffer()
{
    delete[] pCaps;
    delete[] pMats;
    delete pBuffer;
}