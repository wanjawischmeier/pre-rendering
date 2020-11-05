// MathLibrary.h - Contains declarations of math functions
#pragma once

#ifdef MATHLIBRARY_EXPORTS
#define MATHLIBRARY_API __declspec(dllexport)
#else
#define MATHLIBRARY_API __declspec(dllimport)
#endif

#define DECODER __declspec(dllimport)

#include <iostream>
using namespace std;

extern "C" DECODER void ShowImage(string *window, string *path);