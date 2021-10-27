#include "pch.h"
#include "decoder.h"

using namespace std;

ushort* InitializeBuffer(char* samplePath, int* width, int* height, int* size, int* channels)
{
    Mat img = imread(samplePath, IMREAD_UNCHANGED);
    if (*width > 0 || *height > 0)
    {
        image_size = Size(*width, *height);
        resize(img, img, image_size);
        buffer_resize = true;
    }
    else
    {
        *width = img.cols;
        *height = img.rows;
        buffer_resize = false;
    }

    *size = img.total();
    *channels = img.channels();
    buffer_size = (*size) * (*channels);

    pBuffer = new ushort[buffer_size];
    return pBuffer;
}

void ReadToBuffer(char* path)
{
    Mat img = imread(path, IMREAD_UNCHANGED);
    if (buffer_resize)
        resize(img, img, image_size);

    memcpy(pBuffer, img.data, buffer_size);
}

void ReleaseBuffer()
{
    delete pBuffer;
}