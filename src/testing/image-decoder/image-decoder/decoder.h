#pragma once

#define DECODER __declspec(dllimport)

#include <iostream>
#include <opencv2\opencv.hpp>
using namespace std;

extern "C" DECODER unsigned char* imread(char* path, int* width, int* height, size_t* bytes_count);