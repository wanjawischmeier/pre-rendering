#include "pch.h"
#include <combaseapi.h>
#include "decoder.h"
#include <opencv2/opencv.hpp>

using namespace std;
using namespace cv;

static vector<VideoCapture> decoders;


bool initialize(int threads, char* path)
{
    for (size_t i = 0; i < threads; i++)
    {
        decoders.push_back(VideoCapture(path));

        if (!decoders[i].isOpened()) return false;
    }

    return true;
}

bool setFrame(int id, int frame)
{
    decoders[id].set(CAP_PROP_POS_FRAMES, frame);
    return (decoders[id].get(CAP_PROP_POS_FRAMES) == frame);
}

unsigned char* getFrame(int id, int frame, int* bytes_count)
{
    decoders[id].set(CAP_PROP_POS_FRAMES, frame);
    
    Mat tex;
    decoders[id].read(tex);

    return toByteArray(tex, bytes_count);
}

unsigned char* getUnsignedBytes(char* image, int* bytes_count, bool* debug)
{
    Mat img = imread(image);

    return toByteArray(img, bytes_count);
}

void release(int id)
{
    if (id == -1)
    {
        if (decoders.size() != 0)
        {
            for (size_t i = 0; i < decoders.size() - 1; i++)
            {
                decoders[i].release();
            }
        }
        
        decoders.clear();
    }
    else decoders[id].release();
}

int threads()
{
    return decoders.size();
}

int loaded()
{
    int loaded = 0;

    for (size_t i = 0; i < decoders.size() -1; i++)
    {
        if (decoders[i].isOpened()) loaded++;
    }

    return loaded;
}

unsigned char* toByteArray(Mat in, int* bytes_count)
{
    int size = in.total() * in.elemSize();
    unsigned char* raw_bytes = new unsigned char[size];

    memcpy(raw_bytes, in.data, size * sizeof(std::byte));

    *bytes_count = size;

    return raw_bytes;
}