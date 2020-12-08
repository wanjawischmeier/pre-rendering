#pragma once

#define DECODER __declspec(dllimport)

#include <iostream>
using namespace std;

extern "C" DECODER bool initialize(int threads, char* path);
extern "C" DECODER unsigned char* decode(int frame);