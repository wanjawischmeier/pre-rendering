// MathLibrary.cpp : Defines the exported functions for the DLL.
#include "pch.h" // use stdafx.h in Visual Studio 2017 and earlier
#include <opencv2/opencv.hpp>
#include "Math.h"

using namespace std;
using namespace cv;

void ShowImage(string *window, string *path)
{
    cout << "Reading";
    Mat img = imread("C:\\Users\\User\\Pictures\\Wallpaper\\tstimg.jpg");

    imshow("Test Image", img);

    waitKey();
}