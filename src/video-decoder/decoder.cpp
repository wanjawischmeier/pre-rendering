#include "pch.h"
#include "decoder.h"


uchar** InitializeDecoder(
    char *videoPath, int threads,
    FrameCallback frameCallback, ErrorMessage errorCallback,
    VideoInfo &rInfo)
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
        error_callback("Error while opening Video Capture ", ex.what());
        return nullptr;
    }

    rInfo.width = (int)cap.get(CAP_PROP_FRAME_WIDTH);
    rInfo.height = (int)cap.get(CAP_PROP_FRAME_HEIGHT);
    rInfo.fps = (int)cap.get(CAP_PROP_FPS);
    rInfo.frame_count = (size_t)cap.get(CAP_PROP_FRAME_COUNT);
    video_info = rInfo;

    image_size = video_info.width * video_info.height * 3;

    pData = new uchar*[instances];

    for (int i = 0; i < instances; i++)
    {
        pCaps[i] = VideoCapture(cap);
        pMats[i] = Mat(video_info.width, video_info.height, CV_8UC3);
        pData[i] = pMats[i].data;
    }

    return pData;
}

DECODER size_t CurrentFrame(int threadIdx)
{
    VideoCapture cap = pCaps[threadIdx];
    return (size_t)cap.get(CAP_PROP_POS_FRAMES);
}

DECODER bool Seek(size_t frameIdx, int threadIdx)
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
    cap.set(CAP_PROP_POS_FRAMES, (double)frameIdx);

    return true;
}

extern "C" DECODER bool Read(size_t frameIdx, int threadIdx)
{
    VideoCapture cap = pCaps[threadIdx];
    Mat mat = pMats[threadIdx];

    if (!cap.read(mat))
    {
        error_callback("Failed to grab frame", "");
        return false;
    }

    frame_ready(frameIdx, threadIdx);
    return true;
}

extern "C" DECODER bool ReadImage(char* path, int threadIdx)
{
    try
    {
        pMats[threadIdx] = imread(path, cv::IMREAD_COLOR);
    }
    catch (const Exception& ex)
    {
        error_callback("Failed to grab frame", ex.what());
        return false;
    }

    return true;
}

DECODER void ReleaseDecoder()
{
    if (instances == 0) return;

    for (int i = 0; i < instances; i++)
    {
        pCaps[i].release();
        pMats[i].release();
    }

    delete[] pCaps;
    delete[] pMats;
    delete[] pData;
}