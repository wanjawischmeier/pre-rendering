#pragma once

#define DECODER __declspec(dllimport)
#include <stdlib.h>
#include <stdio.h>
#include <png.h>

int width, height;
png_byte color_type;
png_byte bit_depth;
png_bytep* row_pointers = NULL;

void read_png_file(char* filename);
int read_png_file2(char* file_name);
extern "C" DECODER int Test();