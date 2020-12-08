#include "pch.h"
#include <iostream>
#include <opencv2\opencv.hpp>

using namespace std;
using namespace cv;

class Decoder
{
public:
	VideoCapture cap;
	
	Decoder(char* path);
	~Decoder();

	bool isOpened();
	unsigned char* getFrame(int frame);

private:
	Mat frame;

	unsigned char* toBytes();
};
