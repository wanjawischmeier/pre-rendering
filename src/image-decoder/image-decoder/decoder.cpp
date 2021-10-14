#include "pch.h"
#include <combaseapi.h>
#include "decoder.h"

using namespace std;
using namespace cv;

uint16_t* raw_bytes;

void initialize()
{

}

void release()
{
    delete raw_bytes;
}

ushort* imread(char* path, int width, int height, int* channels, int* bytes_count)
{
    Mat img = imread(path, IMREAD_UNCHANGED);
    if (width > 0 || height > 0)
        resize(img, img, Size(width, height));
    // flip(img, img, 0);
    
    *channels = img.channels();
    int size = img.total() * *channels;
    *bytes_count = size;
    size *= sizeof(ushort);
    raw_bytes = new ushort[size];

    memcpy(raw_bytes, img.data, size);

    return raw_bytes;
}