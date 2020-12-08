#include "pch.h"
#include "decoding.h"
#include "decoder.h"

static vector<Decoder*> available;

bool initialize(int threads, char* path)
{
    for (size_t i = 0; i < threads; i++)
    {
        available.push_back(new Decoder(path));

        if (!(*available[i]).isOpened()) return false;
    }

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
