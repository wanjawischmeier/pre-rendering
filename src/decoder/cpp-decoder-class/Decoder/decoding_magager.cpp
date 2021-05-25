#include "pch.h"
#include "decoding_manager.h"
#include "decoder.h"

static vector<Decoder*> decoders;
static vector<Decoder*> available;
static unsigned char* buffer;

bool initialize(char* path, int res_x, int res_y, int threads, int col_channels)
{
    for (size_t i = 0; i < threads; i++)
    {
        decoders.push_back(new Decoder(path));

        if (!(*available[i]).isOpened()) return false;
    }

    buffer = new unsigned char[res_x*res_y*col_channels*threads];

    return true;
}

unsigned char* decode(int frame)
{
    Decoder dec = *available[0];

    if (dec.isOpened())
    {
        unsigned char* bytes = dec.getFrame(frame);

        return bytes;
    }
    else return nullptr;
}

void release()
{
    for (size_t i = 0; i < available.size(); i++)
    {
        delete available[i];
    }
    available.clear();

    delete[] buffer;
}