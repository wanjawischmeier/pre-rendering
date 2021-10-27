#pragma once

#define DECODER __declspec(dllimport)

#include <iostream>
#include <opencv2\opencv.hpp>

using namespace std;
using namespace cv;

ushort* pBuffer;  // Pointer to the image buffer
bool buffer_resize; // Wether the decoded images should be resized
Size image_size;    // If so, to which resolution (is null if 'buffer_resize' is false).
int buffer_size;    // The total size of the buffer in bytes.

/// <summary>
/// Initializes the buffer.
/// </summary>
/// <param name="samplePath">The file path of a sample image (for getting the image size).</param>
/// <param name="width">
/// The desired width to which all textures should be resized.
/// If the value is -1, it will get set to the actual width of the sample image.
/// The buffer will also use the actual width of the sample image in this case.
/// </param>
/// <param name="height">Same as with the width parameter.</param>
/// <param name="size">Will be set to the total size of the buffer in bytes.</param>
/// <returns>Returns a pointer to the buffer that images decoded using the 'ReadToBuffer' function will be written to.</returns>
extern "C" DECODER ushort* InitializeBuffer(char* samplePath, int* width, int* height, int* size, int* channels);

/// <summary>
/// Decodes an image and writes it into the currently active buffer.
/// </summary>
/// <param name="path">The path to the image</param>
extern "C" DECODER void ReadToBuffer(char* path);

/// <summary>
/// Releases the currently active buffer.
/// </summary>
extern "C" DECODER void ReleaseBuffer();