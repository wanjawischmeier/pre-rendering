#pragma once

#define DECODER __declspec(dllimport)

#include <iostream>
using namespace std;

extern "C" DECODER void ShowImage(string * window, string * path);
extern "C" DECODER bool Create(string * mapFile);
extern "C" DECODER double SetFrame(double * index);
extern "C" DECODER void Destroy();