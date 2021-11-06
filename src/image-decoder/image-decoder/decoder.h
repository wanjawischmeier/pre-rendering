#pragma once

#define DECODER __declspec(dllimport)

#include <iostream>
#include <opencv2\opencv.hpp>

using namespace std;
using namespace cv;

ushort* pBuffer;		// Pointer to the image buffer
bool buffer_resize;		// Wether the decoded images should be resized
Size image_resolution;	// If so, to which resolution
size_t image_size;		// The total size of an image
size_t buffer_depth;	// The amount of images in the buffer

extern "C" DECODER ushort* InitializeBuffer(char* samplePath, int* width, int* height, int depth);
extern "C" DECODER bool ReadToBuffer(char* path, int index);
extern "C" DECODER void ReleaseBuffer();