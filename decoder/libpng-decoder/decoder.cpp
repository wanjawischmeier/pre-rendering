#include "pch.h"
#include "decoder.h"
void empty() { }

png_bytepp initialize(char* path, int _instances)
{
    instances = _instances;

    if (fopen_s(&fp, path, "rb") != 0)
        return 0;

    png_structp png_ptr = png_create_read_struct(PNG_LIBPNG_VER_STRING, NULL, NULL, NULL);
    if (!png_ptr)
        return 0;

    png_infop info_ptr = png_create_info_struct(png_ptr);
    if (!info_ptr) {
        png_destroy_read_struct(&png_ptr, NULL, NULL);
        return 0;
    }

    png_init_io(png_ptr, fp);
    png_read_info(png_ptr, info_ptr);


    png_uint_32 width, height;
    int color_type, bit_depth, channels;
    png_get_IHDR(png_ptr, info_ptr, &width, &height, &bit_depth, &color_type, NULL, NULL, NULL);
    channels = png_get_channels(png_ptr, info_ptr);

    row_pointers = (png_bytepp)png_malloc(png_ptr, sizeof(png_bytepp) * height);
    for (png_uint_32 j = 0; j < height; j++) {
        row_pointers[j] = (png_bytep)png_malloc(png_ptr, width * channels * sizeof(USHORT));
    }

    png_destroy_read_struct(&png_ptr, &info_ptr, NULL);
    return row_pointers;
}

void release()
{
    free(row_pointers);
}

int read_png(char* path, int index)
{
    if (fopen_s(&fp, path, "rb") != 0)
        return false;

    png_structp png_ptr = png_create_read_struct(
        PNG_LIBPNG_VER_STRING, NULL, NULL, NULL
    );
    if (!png_ptr)
        return false;

    png_infop info_ptr = png_create_info_struct(png_ptr);
    if (!info_ptr)
        return false;

    png_init_io(png_ptr, fp);

    png_read_info(png_ptr, info_ptr);
    png_read_image(png_ptr, row_pointers);

    // png_destroy_read_struct(&png_ptr, &info_ptr, NULL);
    return true;
}