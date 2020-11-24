#pragma once

#define DECODER __declspec(dllimport)

#include <iostream>
#include <opencv2\opencv.hpp>
using namespace std;

// extern "C" DECODER void GetDecoderID(string * window, string * path);

extern "C" DECODER void Initialize(int * threads);
extern "C" DECODER int Create(string * mapFile);
extern "C" DECODER double SetFrame(int* id, double * index);
extern "C" DECODER void ShowCustomImage(string * window, string * path);
extern "C" DECODER void ShowImage(int* threads, string * window);
extern "C" DECODER unsigned char** GetImage(int* id);
extern "C" DECODER char* GetBytes(int* id, char * testimage);
extern "C" DECODER void Destroy(int * id);
extern "C" DECODER char* marshal(char* in);
extern "C" DECODER char* marshalwithsize(char* in, size_t size);