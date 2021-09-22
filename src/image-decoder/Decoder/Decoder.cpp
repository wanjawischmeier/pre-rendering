#include "pch.h"
#include <combaseapi.h>
#include "decoder.h"
#include <opencv2/opencv.hpp>

using namespace std;
using namespace cv;

unsigned char* GetUnsignedBytes(char* image, int* bytes_count, bool* debug)
{
    if (debug) cout << "Reading..." << endl;

    Mat img = imread(image);

    size_t size = img.total() * img.elemSize();
    unsigned char* raw_bytes = new unsigned char[size];

    if (debug) cout << "Copying " + to_string(size) + " bytes..." << endl;

    memcpy(raw_bytes, img.data, size * sizeof(std::byte));

    // bytes_count as size_t
    *bytes_count = size;

    return raw_bytes;
}
