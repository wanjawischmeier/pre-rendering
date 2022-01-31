#pragma once
#define DECODER __declspec(dllexport)
typedef size_t(__stdcall* FrameReady)(size_t);

#include "opencv2/opencv.hpp"

using namespace std;
using namespace cv;




struct VideoInfo
{
	int width, height, fps;
	size_t frame_count;
};

ushort* pBuffer;		// Pointer to the image buffer
VideoCapture color, maps;
Mat color_mat, maps_mat;
FrameReady frame_ready;
bool resize_image = false;
Size image_resolution;	// If so, to which resolution
size_t image_size;		// The total size of an image
size_t maps_offset;



/*
extern "C" DECODER bool InitializeBuffer(
	char* videoPath, FrameReady callback,
	int width, int height, int depth,
	VideoInfo* info, ushort* buffer);
extern "C" DECODER bool ReadToBuffer(size_t frame);
extern "C" DECODER bool ReleaseBuffer();*/
extern "C" DECODER VideoInfo Test(FrameReady callback, VideoInfo& test);