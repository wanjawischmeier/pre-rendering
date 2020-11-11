#include "pch.h"
#include "decoder.h"
#include <opencv2/opencv.hpp>

using namespace std;
using namespace cv;

static VideoCapture* caps_;
static int threads_;
static int c_thread_;
static byte* c_image_;


void Initialize(int* threads)
{
    cout << "Initializing " + to_string(*threads) + " threads..." << endl;
    threads_ = *threads;
    c_thread_ = 0;

    caps_ = new VideoCapture[*threads];
}

/// <summary>
/// Create a new Decoder instance. Will return the id of this instance.
/// </summary>
/// <param name="mapFile"></param>
/// <returns></returns>
int Create(string* mapFile)
{    
    caps_[c_thread_] = *new VideoCapture(*mapFile);
    
    if (!caps_[c_thread_].isOpened())
    {
        cout << "Error opening video stream or file (" + *mapFile + ")" << endl;
        
        return -1;
    }

    return c_thread_;
}

double SetFrame(int* id, double* index)
{
    caps_[c_thread_].set(CAP_PROP_POS_FRAMES, *index);
    return caps_[c_thread_].get(CAP_PROP_POS_FRAMES);
}

void ShowCustomImage(string* window, string* path)
{
    cout << "Reading custom image...";
    Mat img = imread("C:\\Users\\User\\Pictures\\Wallpaper\\tstimg.jpg");

    imshow("Test Image", img);

    waitKey();
}

void ShowImage(int* id, string* window)
{
    cout << "Reading...";
    Mat img = imread("C:\\Users\\User\\Pictures\\Wallpaper\\tstimg.jpg");

    imshow("Test Image", img);

    waitKey();
}

void GetImage(int* id)
{
    cout << "Reading...";
    Mat img = imread("C:\\Users\\User\\Pictures\\Wallpaper\\tstimg.jpg");

    cout << "Extracting bytes ";
    int size = img.total() * img.elemSize();
    cout << "(at " + to_string(sizeof(byte)) + "bits) from image of size " + to_string(size) + "..." << endl;
    c_image_ = new byte[size];
    cout << "Copying bytes..." << endl;
    memcpy(c_image_, img.data, size * sizeof(byte));
    char str[sizeof(*c_image_) +1];
    memcpy(str, c_image_, sizeof(*c_image_));
    str[sizeof(*c_image_)] = 0;
    cout << str;
    // return c_image_;
}

void Destroy(int* id)
{
    free(caps_);
    free(c_image_);
    // delete(caps_);
    // delete(c_image_);
}