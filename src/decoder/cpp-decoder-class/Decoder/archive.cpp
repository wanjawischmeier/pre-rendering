#include "pch.h"
#include <combaseapi.h>
#include "archive.h"
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
    unsigned char** bytes = new unsigned char* [size];  // you will have to delete[] that later
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
    cout << testimage << endl;

    //Mat img = imread("C:\\Users\\User\\Pictures\\Wallpapers\\tst3.jpg");
    Mat img = imread(testimage);
    //vector<uchar> buffer;

    //imencode(".jpg", img, buffer);
    //char* test = reinterpret_cast<char*>(buffer.data());

    int size = img.total() * img.elemSize();
    char* bytes = new char[size];
    std::byte* raw_bytes = new std::byte[size];

    cout << "Copying " + to_string(strlen(bytes)) + " bytes..." << endl;

    //Casting
    memcpy(bytes, img.data, size * sizeof(std::byte));
    memcpy(raw_bytes, img.data, size * sizeof(std::byte));
    char* bytes_ccast = (char*)(img.data);
    char* bytes_rcast = reinterpret_cast<char*>(img.data);


    cout << "total_size: " + to_string(img.total()) << endl;
    cout << "elem_size: " + to_string(img.elemSize()) << endl;
    cout << "byte_size: " + to_string(sizeof(std::byte)) << endl;
    cout << "data_size: " + to_string(sizeof(img.data)) << endl;
    cout << "bytes_size: " + to_string(strlen(bytes)) << endl;
    cout << "raw_bytes_size: " + to_string(sizeof(raw_bytes)) << endl;
    cout << "ccast_size: " + to_string(strlen(bytes_ccast)) << endl;
    cout << "rcast_size: " + to_string(strlen(bytes_rcast)) << endl;
    //cout << buffer.size() << endl;
    //cout << bytes[img.total()+1];
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
    //return marshalwithsize(test, buffer.size());
    return marshal(bytes);
}

unsigned char* GetUnsigned_Bytes(int* id, char* testimage, int* bytes_count)
{
    /*
    cout << "Reading ";
    cout << testimage << endl;
    */
    Mat img = imread(testimage);

    int size = img.total() * img.elemSize();
    unsigned char* raw_bytes = new unsigned char[size];

    // cout << "Copying " + to_string(size) + " bytes..." << endl;

    memcpy(raw_bytes, img.data, size * sizeof(std::byte));

    *bytes_count = size;

    return raw_bytes;
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

unsigned char* marshalu(unsigned char* in)
{
    size_t stSize = sizeof(in) + sizeof(char);
    unsigned char* pszReturn = NULL;

    pszReturn = (unsigned char*)::CoTaskMemAlloc(stSize);
    // memcpy_s(pszReturn, stSize, in);
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

unsigned char* getUnsignedBytes(char* image, int* bytes_count, bool* debug)
{
    if (debug) cout << "Reading..." << endl;

    Mat img = imread(image);

    int size = img.total() * img.elemSize();
    unsigned char* raw_bytes = new unsigned char[size];

    if (debug) cout << "Copying " + to_string(size) + " bytes..." << endl;
    memcpy(raw_bytes, img.data, size * sizeof(std::byte));

    *bytes_count = size;

    if (debug) cout << "Reading done, returning pointer to bytes" << endl;
    return raw_bytes;
}