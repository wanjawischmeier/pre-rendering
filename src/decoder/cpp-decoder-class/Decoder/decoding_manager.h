#pragma once

#define DECODER __declspec(dllimport)

#include <iostream>
using namespace std;

extern "C" DECODER bool initialize(char* path, int res_x, int res_y, int threads, int col_channels = 3);
extern "C" DECODER unsigned char* decode(int frame);
extern "C" DECODER void release();