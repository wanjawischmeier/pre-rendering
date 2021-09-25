#pragma once

#define DECODER __declspec(dllimport)

#include <iostream>
#include <opencv2\opencv.hpp>

extern "C" DECODER unsigned char* GetUnsignedBytes(char* image, int* bytes_count, bool* debug);