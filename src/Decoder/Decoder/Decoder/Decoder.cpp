#include "pch.h"
#include "Decoder.h"
#include <opencv2/opencv.hpp>

using namespace std;
using namespace cv;

static string mapFile_;
static VideoCapture cap_;

void ShowImage(string* window, string* path)
{
    cout << "Reading...";

    Mat img = imread("C:\\Users\\User\\Pictures\\Wallpaper\\tstimg.jpg");

    imshow("Test Image", img);

    waitKey();
}

bool Create(string* mapFile)
{
    mapFile_ = *mapFile;
    
    cap_ = *new VideoCapture(mapFile_);

    if (!cap_.isOpened())
    {
        cout << "Error opening video stream or file (" + mapFile_ + ")" << endl;
        
        return -1;
    }

    return 0;
}

double SetFrame(double* index)
{
    cap_.set(CAP_PROP_POS_FRAMES, *index);
    return cap_.get(CAP_PROP_POS_FRAMES);
}

void Destroy()
{

}

/*
Decoder::Decoder(string* mapFile)
{
    this->map = *mapFile;
}

Decoder::~Decoder()
{
    cout << "Deleted " + this->map;
}

void Decoder::ReadFrame(int* frame)
{
    cout << "Not implemented yet";
}

Decoder* CreateDecoder(string* mapFile)
{
    return new Decoder(mapFile);
}
*/