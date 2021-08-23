#include "pch.h"
#include "decoding_manager.h"
/*
#include "decoder.h"

static vector<Decoder*> decoders;
static vector<Decoder*> available;
*/
int res_x, res_y, threads, col_channels;

VideoCapture* caps;
vector<int> available;
unsigned char* buffer;

extern "C" bool initialize(char* path, int _res_x, int _res_y, int _threads, int _col_channels)
{
    res_x = _res_x; res_y = _res_y;
    threads = _threads;
    col_channels = _col_channels;

    caps = new VideoCapture[threads];
    buffer = new unsigned char[res_x * res_y * col_channels * threads];

    for (size_t i = 0; i < threads; i++)
    {
        caps[i].open(path);

        if (!(caps[i]).isOpened()) return false;
    }

    return true;
}

unsigned char* decode(int frame)
{
    int threadIdx = available[0];
    VideoCapture dec = caps[threadIdx];

    if (dec.isOpened())
    {
        unsigned char* out_bytes = new unsigned char[res_x * res_y * col_channels];
        getFrame(dec, frame, threadIdx, out_bytes);

        return out_bytes;
    }
    else return nullptr;
}

void release()
{
    delete[] caps;
    delete[] buffer;
}

void getFrame(VideoCapture cap, int frameIdx, int threadIdx, unsigned char* out_bytes)
{
    Mat frame;
    int offset = res_x * res_y * col_channels * threadIdx;

    cap.set(CAP_PROP_POS_FRAMES, frameIdx);

    cap.read(frame);

    size_t size = frame.total() * frame.elemSize();

    memcpy(out_bytes + offset, frame.data, size * sizeof(std::byte));
}

void bytecpy(unsigned char* target, Mat frame, int offset)
{
    size_t size = frame.total() * frame.elemSize();

    memcpy(target + offset, frame.data, size * sizeof(std::byte));
}