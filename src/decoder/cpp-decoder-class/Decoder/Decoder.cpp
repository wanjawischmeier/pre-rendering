#include "pch.h"
#include "decoder.h"
#include <opencv2/opencv.hpp>

using namespace std;
using namespace cv;

static VideoCapture* caps_;
static int threads_;
static int c_thread_;
static Mat c_img_;
//static byte* c_image_;


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

void GetImage(unsigned char* *data, int *size, int* id)
{
    cout << "Reading..." << endl;
    c_img_ = imread("C:\\Users\\User\\Pictures\\Wallpaper\\tstimg.jpg");

    cout << "Extracting bytes ";
    //byte[] raw = new byte[(int)(c_img_.total() * c_img_.channels())];
    uchar* arr = c_img_.isContinuous() ? c_img_.data : c_img_.clone().data;
    uint length = c_img_.total() * c_img_.channels();

    *data = arr;
    //*size = length;
    /*
    int size = img.total() * img.elemSize();
    cout << "(at " + to_string(sizeof(byte)) + "bits) from image of size " + to_string(size) + "..." << endl;
    
    //byte* c_image_ = new byte[size];
    cout << "Copying bytes..." << endl;
    //memcpy(target, img.data, size * sizeof(byte));
    */
    //memcpy(*data, c_img_.data, sizeof(*c_img_.data));
    //*data = c_img_.data;
    //*size = sizeof(*c_img_.data);
    //char str[sizeof(*img.data) +1];
    //memcpy(str, img.data, sizeof(*img.data));
    //str[sizeof(*img.data)] = 0;
    //cout << str;
    
    // return c_image_;
}

void Destroy(int* id)
{
    free(caps_);
    //free(c_image_);
    // delete(caps_);
    // delete(c_image_);
}