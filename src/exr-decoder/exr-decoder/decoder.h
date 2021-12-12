#pragma once

#define DECODER __declspec(dllexport)

#include <cstdio>
#include <cstdlib>
#include <vector>

#define TINYEXR_IMPLEMENTATION
#include "tinyexr/tinyexr.h"

struct ImageInfo
{
	int channels, width, height, multipart, tiled;
};


int Decode(char* path);
extern "C" DECODER int GetImageInfo(char* path, ImageInfo* info);
extern "C" DECODER int GetMultipartImageInfo(char* path, ImageInfo* info);
extern "C" DECODER int CombineToMultipart(char** paths, char* targetPath, int length, EXRImage** images);
extern "C" DECODER void ReleaseHeader(EXRHeader* header);
extern "C" DECODER void ReleaseImage(EXRImage* image);