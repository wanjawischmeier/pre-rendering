#pragma once

#define DECODER __declspec(dllexport)
#include <stddef.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <png.h>
#include <zlib.h>

FILE* fp;
png_bytepp row_pointers;
int instances;

extern "C" DECODER void empty();
extern "C" DECODER png_bytepp initialize(char* path, int _instances);
extern "C" DECODER void release();
extern "C" DECODER int read_png(char* path, int index);