#pragma once
#define DECODER __declspec(dllexport)

typedef void(__stdcall* FrameCallback)(size_t, int, int);
typedef void(__stdcall* ErrorMessage)(const char*, const char*);

#include "opencv2/opencv.hpp"

using namespace std;
using namespace cv;


struct VideoInfo
{
	int width, height, fps;
	size_t frame_count;
};

VideoCapture *pCaps;
Mat *pMats;
uchar** pData;
FrameCallback frame_ready;
ErrorMessage error_callback;
VideoInfo video_info;
size_t image_size;		// The total size of an image
int instances;


extern "C" DECODER uchar** InitializeBuffer(
	char* videoPath, int threads,
	FrameCallback frameCallback, ErrorMessage errorCallback,
	VideoInfo &rInfo);
extern "C" DECODER bool ReadToBuffer(size_t frameIdx, int threadIdx, int bufferIdx);
extern "C" DECODER void ReleaseBuffer();