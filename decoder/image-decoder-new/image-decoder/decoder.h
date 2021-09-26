#pragma once

#define DECODER __declspec(dllimport)

#include <iostream>
#include <opencv2\opencv.hpp>
using namespace std;

extern "C" DECODER void initialize();
extern "C" DECODER void release();
extern "C" DECODER uint16_t* imread_old(char* path, int* width, int* height, ushort* channels, size_t * bytes_count);
extern "C" DECODER void imread(
	char* path, int* width, int* height,
	unsigned char* color, // unsigned char* depth,
	size_t* size // , size_t* depth_size
);