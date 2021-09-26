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

uint16_t* imread_old(char* path, int* width, int* height, ushort* channels, size_t* bytes_count)
{
    Mat img = imread(path, IMREAD_UNCHANGED);
    resize(img, img, Size(*width, *height));
    flip(img, img, 0);

    *channels = img.channels();
    size_t size = img.total() * *channels;
    *bytes_count = size;
    size *= sizeof(uint16_t);
    raw_bytes = new uint16_t[size];

    memcpy(raw_bytes, img.data, size);

    return raw_bytes;
}

void imread(
    char* path, int* width, int* height,
    unsigned char* color, // unsigned char* depth,
    size_t* size // , size_t* depth_size
)
{
    Mat col; // , all, channels[4];
    col = imread(path);
    // all = imread(path, IMREAD_UNCHANGED);
    resize(col, col, Size(*width, *height));
    flip(col, col, 0);
    
    // split(all, channels);

    size_t tsize = col.total() * col.elemSize();
    unsigned char* raw_bytes = new unsigned char[tsize];
    // unsigned char[] raw_bytes = new unsigned char[tsize];

    memcpy(raw_bytes, col.data, tsize * sizeof(byte));
    *size = tsize;
    color = raw_bytes;

    // waitKey(0);
}