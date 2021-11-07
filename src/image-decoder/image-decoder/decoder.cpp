#include "pch.h"
#include "decoder.h"

using namespace std;

ushort* InitializeBuffer(char* samplePath, int* width, int* height, int depth)
{
    Mat img = imread(samplePath, IMREAD_UNCHANGED);

    if (img.empty() || img.channels() != 4)
        return nullptr;

    if (*width > 0 || *height > 0)
    {
        image_resolution = Size(*width, *height);
        resize(img, img, image_resolution);
        buffer_resize = true;
    }
    else
    {
        *width = img.cols;
        *height = img.rows;
        buffer_resize = false;
    }

    image_size = img.total() * 4;
    buffer_depth = depth;

    pBuffer = new ushort[image_size * buffer_depth];
    buffer_allocated = true;
    return pBuffer;
}

bool ReadToBuffer(char* path, int index)
{
    Mat img = imread(path, IMREAD_UNCHANGED);
    if (img.empty()) return false;

    if (buffer_resize)
        resize(img, img, image_resolution);

    size_t startIndex = (size_t)index * image_size;
    memcpy(&pBuffer[startIndex], img.data, image_size * sizeof(ushort));

    return true;
}

bool ReleaseBuffer()
{
    if (!buffer_allocated) return false;

    delete pBuffer;
    return true;
}