#pragma once

#define DECODER __declspec(dllimport)

#include <iostream>
#include <opencv2\opencv.hpp>

using namespace std;
using namespace cv;

extern "C" DECODER bool initialize(char* path, int res_x, int res_y, int threads, int col_channels = 3);
extern "C" DECODER unsigned char* decode(int frame);
extern "C" DECODER void release();

void getFrame(VideoCapture cap, int frameIdx, int threadIdx, unsigned char* out_bytes);
