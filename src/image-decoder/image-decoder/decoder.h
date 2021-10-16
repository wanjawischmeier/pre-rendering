#pragma once

#define DECODER __declspec(dllimport)

#include <iostream>
#include <opencv2\opencv.hpp>
using namespace std;

extern "C" DECODER void initialize();
extern "C" DECODER void release();
extern "C" DECODER ushort* imread(char* path, int width, int height, int* channels, int* bytes_count);