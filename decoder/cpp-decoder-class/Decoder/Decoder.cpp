#include "pch.h"
#include <combaseapi.h>
#include <opencv2/opencv.hpp>
#include "decoder.h"

using namespace std;
using namespace cv;

Decoder::Decoder(char* path)
{
	cap = VideoCapture(path);
}

Decoder::~Decoder()
{
	cap.release();
}

bool Decoder::isOpened()
{
	return cap.isOpened();
}

unsigned char* Decoder::getFrame(int frameIdx)
{
	cap.set(CAP_PROP_POS_FRAMES, frameIdx);

	cap.read(frame);

	return toBytes(frame);
}

unsigned char* toBytes(Mat frame)
{
	int size = frame.total() * frame.elemSize();
	unsigned char* raw_bytes = new unsigned char[size];

	memcpy(raw_bytes, frame.data, size * sizeof(std::byte));

	return raw_bytes;
}
