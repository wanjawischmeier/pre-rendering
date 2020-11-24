#include "pch.h"
#include <combaseapi.h>
#include "decoder.h"
#include <opencv2/opencv.hpp>

using namespace std;
using namespace cv;

static VideoCapture* caps_;
static int threads_;
static int c_thread_;
//static Mat* c_img_;
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

unsigned char** GetImage(int* id)
{
    cout << "Reading..." << endl;
    Mat c_img_ = imread("C:\\Users\\User\\Pictures\\Wallpaper\\tstimg.jpg");

    cout << "Extracting bytes ";


    int size = c_img_.total() * c_img_.elemSize();
    unsigned char** bytes = new unsigned char*[size];  // you will have to delete[] that later
    memcpy(bytes, c_img_.data, size * sizeof(std::byte));
    //memcpy(*data, c_img_.data, size * sizeof(byte));
    //data = bytes;

    //string s(reinterpret_cast<char const*>(bytes), size * sizeof(byte));
    //cout << s << endl;

    return nullptr;
}

char* GetBytes(int* id, char* testimage)
{
    cout << "Reading ";
    //string test = *reinterpret_cast<string*>(testimage);
    cout << testimage;

    Mat img = imread("C:\\Users\\User\\Pictures\\Wallpapers\\tst3.jpg");
    vector<uchar> buffer;

    imencode(".jpg", img, buffer);
    char* test = reinterpret_cast<char*>(buffer.data());

    int size = img.total() * img.elemSize();
    cout << to_string(size) + " bytes..." << endl;
    char* bytes = new char[size];
    char* bytes_two = (char*)(img.data);
    cout << "Copying " + to_string(strlen(bytes_two)) + " bytes..." << endl;
    memcpy(bytes, img.data, size * sizeof(std::byte));
    
    cout << to_string(size) << endl;
    cout << to_string(img.total()) << endl;
    cout << to_string(img.elemSize()) << endl;
    cout << to_string(sizeof(std::byte)) << endl;
    cout << to_string(sizeof(bytes)) << endl;
    cout << to_string(sizeof(img.data)) << endl;
    cout << buffer.size() << endl;
    cout << test[100000];
    /*
    char* conv = new char[buffer.size()];
    for (size_t i = 0; i < buffer.size(); i++)
    {
        conv[i] = buffer[i];
        cout << conv[i];
    }
    cout << conv;
    //cout << ;
    */
    return marshalwithsize(test, buffer.size());
}

void Destroy(int* id)
{
    // delete(caps_);
    // free(c_image_);
    // delete(caps_);
    // delete(c_image_);
}

char* marshal(char* in)
{
    size_t stSize = strlen(in) + sizeof(char);
    char* pszReturn = NULL;

    pszReturn = (char*)::CoTaskMemAlloc(stSize);
    strcpy_s(pszReturn, stSize, in);
    return pszReturn;
}

char* marshalwithsize(char* in, size_t size)
{
    char* pszReturn = NULL;

    pszReturn = (char*)::CoTaskMemAlloc(size);
    strcpy_s(pszReturn, size, in);
    cout << "conv_size: " + to_string(strlen(pszReturn)) << endl;
    return pszReturn;
}
