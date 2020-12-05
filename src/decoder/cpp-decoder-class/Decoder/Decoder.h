#pragma once

#define DECODER __declspec(dllimport)

#include <iostream>
#include <opencv2\opencv.hpp>
using namespace std;

extern "C" DECODER int newDecoder(char* path);
extern "C" DECODER bool setFrame(int id, int frame);
extern "C" DECODER unsigned char* getFrame(int id, int frame, int* bytes_count);
extern "C" DECODER void release(int id = -1);

extern "C" DECODER unsigned char* toByteArray(cv::Mat in, int* bytes_count);
extern "C" DECODER unsigned char* getUnsignedBytes(char* image, int* bytes_count, bool* debug);