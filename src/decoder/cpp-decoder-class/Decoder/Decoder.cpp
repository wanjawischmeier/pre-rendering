#include "pch.h"
#include <combaseapi.h>
#include "decoder.h"
#include <opencv2/opencv.hpp>

using namespace std;
using namespace cv;

static vector<VideoCapture> decoders;


int newDecoder(char* path)
{
    try
    {
        decoders.push_back(VideoCapture(path));

        return decoders.size() -1;
    }
    catch (const std::exception&)
    {
        return -1;
    }
}

bool setFrame(int id, int frame)
{
    decoders[id].set(CAP_PROP_POS_FRAMES, frame);
    return (decoders[id].get(CAP_PROP_POS_FRAMES) == frame);
}

unsigned char* getFrame(int id, int frame, int* bytes_count)
{
    decoders[id].set(CAP_PROP_POS_FRAMES, frame);
    decoders[id].grab();

    Mat tex;
    decoders[id].read(tex);
    return toByteArray(tex, bytes_count);
}

unsigned char* toByteArray(Mat in, int* bytes_count)
{
    int size = in.total() * in.elemSize();
    unsigned char* raw_bytes = new unsigned char[size];

    memcpy(raw_bytes, in.data, size * sizeof(std::byte));

    *bytes_count = size;

    return raw_bytes;
}

unsigned char* getUnsignedBytes(char* image, int* bytes_count, bool* debug)
{
    if (debug) cout << "Reading..." << endl;

    Mat img = imread(image);

    int size = img.total() * img.elemSize();
    unsigned char* raw_bytes = new unsigned char[size];

    if (debug) cout << "Copying " + to_string(size) + " bytes..." << endl;
    memcpy(raw_bytes, img.data, size * sizeof(std::byte));

    *bytes_count = size;

    if (debug) cout << "Reading done, returning pointer to bytes" << endl;
    return raw_bytes;
}
