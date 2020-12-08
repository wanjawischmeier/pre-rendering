#include "pch.h"
#include <combaseapi.h>
#include <opencv2/opencv.hpp>
#include "decoder.h"

using namespace std;
using namespace cv;

Decoder::Decoder(char* path)
{
	this->cap = VideoCapture(path);
}

Decoder::~Decoder()
{
	this->cap.release();
}

bool Decoder::isOpened()
{
	return this->cap.isOpened();
}

unsigned char* Decoder::getFrame(int frame)
{
	this->cap.set(CAP_PROP_POS_FRAMES, frame);

	this->cap.read(this->frame);

	return toBytes();
}

unsigned char* Decoder::toBytes()
{
	int size = this->frame.total() * this->frame.elemSize();
	unsigned char* raw_bytes = new unsigned char[size];

	memcpy(raw_bytes, this->frame.data, size * sizeof(std::byte));

	return raw_bytes;
}
