#include "pch.h"
#include <combaseapi.h>
#include "decoder.h"

using namespace std;
using namespace cv;

unsigned char* imread(char* path, int* width, int* height, size_t* bytes_count)
{
    Mat img = imread(path);
    resize(img, img, Size(*width, *height));
    flip(img, img, 0);
    
    size_t size = img.total() * img.elemSize();
    unsigned char* raw_bytes = new unsigned char[size];

    memcpy(raw_bytes, img.data, size * sizeof(byte));
    *bytes_count = size;

    waitKey(0);
    return raw_bytes;
}