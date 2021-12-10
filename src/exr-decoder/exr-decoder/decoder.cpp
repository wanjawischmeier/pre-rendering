#include "pch.h"
#include "decoder.h"

int Decode(char* path)
{
    // 1. Read EXR version.
    EXRVersion exr_version;

    int ret = ParseEXRVersionFromFile(&exr_version, path);
    if (ret != 0) {
        fprintf(stderr, "Invalid EXR file: %s\n", path);
        return -1;
    }

    if (!exr_version.multipart) {
        // must be multipart flag is true.
        return -1;
    }

    // 2.  EXR headers in the EXR.
    EXRHeader** exr_headers; // list of EXRHeader pointers.
    int num_exr_headers;
    const char* err = nullptr; // or nullptr in C++11 or later

    // Memory for EXRHeader is allocated inside of ParseEXRMultipartHeaderFromFile,
    ret = ParseEXRMultipartHeaderFromFile(&exr_headers, &num_exr_headers, &exr_version, path, &err);
    if (ret != 0) {
        fprintf(stderr, "Parse EXR err: %s\n", err);
        FreeEXRErrorMessage(err); // free's buffer for an error message
        return ret;
    }

    printf("num parts = %d\n", num_exr_headers);


    // 3. Load images.

    // Prepare array of EXRImage.
    std::vector<EXRImage> images(num_exr_headers);
    for (int i = 0; i < num_exr_headers; i++) {
        InitEXRImage(&images[i]);
    }

    ret = LoadEXRMultipartImageFromFile(&images.at(0), const_cast<const EXRHeader**>(exr_headers), num_exr_headers, path, &err);
    if (ret != 0) {
        fprintf(stderr, "Parse EXR err: %s\n", err);
        FreeEXRErrorMessage(err); // free's buffer for an error message
        return ret;
    }
    
    printf("Loaded %d part images\n", num_exr_headers);

    // 4. Access image data
    // `exr_image.images` will be filled when EXR is scanline format.
    // `exr_image.tiled` will be filled when EXR is tiled format.
    

    // 5. Free images
    for (int i = 0; i < num_exr_headers; i++) {
        FreeEXRImage(&images.at(i));
    }

    // 6. Free headers.
    for (int i = 0; i < num_exr_headers; i++) {
        FreeEXRHeader(exr_headers[i]);
        free(exr_headers[i]);
    }
    free(exr_headers);

    return true;
}

DECODER int GetImageInfo(char* path, ImageInfo* info)
{
    // 1. Read EXR version.
    EXRVersion exr_version;

    int ret = ParseEXRVersionFromFile(&exr_version, path);
    if (ret != 0) {
        fprintf(stderr, "Invalid EXR file: %s\n", path);
        return -10;
    }

    if (exr_version.multipart) {
        // must be multipart flag is true.
        return -20;
    }

    // 2. Read EXR header
    EXRHeader exr_header;
    InitEXRHeader(&exr_header);

    const char* err = NULL; // or `nullptr` in C++11 or later.
    ret = ParseEXRHeaderFromFile(&exr_header, &exr_version, path, &err);
    if (ret != 0) {
        fprintf(stderr, "Parse EXR err: %s\n", err);
        FreeEXRErrorMessage(err); // free's buffer for an error message
        return ret;
    }

    info->channels = exr_header.num_channels;
    info->multipart = exr_header.multipart;
    info->tiled = exr_header.tiled;

    EXRImage exr_image;
    InitEXRImage(&exr_image);

    ret = LoadEXRImageFromFile(&exr_image, &exr_header, path, &err);
    if (ret != 0) {
        fprintf(stderr, "Load EXR err: %s\n", err);
        FreeEXRHeader(&exr_header);
        FreeEXRErrorMessage(err); // free's buffer for an error message
        return ret;
    }

    info->width = exr_image.width;
    info->height = exr_image.height;

    // 3. Free header.
    FreeEXRImage(&exr_image);
    FreeEXRHeader(&exr_header);

    return 1;
}

DECODER int GetMultipartImageInfo(char* path, ImageInfo* info)
{
    // 1. Read EXR version.
    EXRVersion exr_version;

    int ret = ParseEXRVersionFromFile(&exr_version, path);
    if (ret != 0) {
        fprintf(stderr, "Invalid EXR file: %s\n", path);
        return -10;
    }

    if (!exr_version.multipart) {
        // must be multipart flag is true.
        return -20;
    }

    // 2.  EXR headers in the EXR.
    EXRHeader** exr_headers; // list of EXRHeader pointers.
    int num_exr_headers;
    const char* err = nullptr;

    // Memory for EXRHeader is allocated inside of ParseEXRMultipartHeaderFromFile,
    ret = ParseEXRMultipartHeaderFromFile(&exr_headers, &num_exr_headers, &exr_version, path, &err);
    if (ret != 0) {
        fprintf(stderr, "Parse EXR err: %s\n", err);
        FreeEXRErrorMessage(err); // free's buffer for an error message
        return ret;
    }

    printf("num parts = %d\n", num_exr_headers);

    info->channels = exr_headers[0]->num_channels;
    info->width = exr_headers[0]->tile_size_x;
    info->height = exr_headers[0]->tile_size_y;
    info->multipart = exr_headers[0]->multipart;
    info->tiled = exr_headers[0]->tiled;

    // 3. Free headers.
    for (int i = 0; i < num_exr_headers; i++) {
        FreeEXRHeader(exr_headers[i]);
        free(exr_headers[i]);
    }
    free(exr_headers);


    return 1;
}

DECODER int CombineToMultipart(char** paths, char* targetPath, int length, EXRImage** images)
{
    EXRHeader* exr_headers = new EXRHeader[length];
    EXRImage* exr_images = new EXRImage[length];
    int ret;
    const char* err = nullptr;

    for (size_t i = 0; i < length; i++)
    {
        // 1. Read EXR version.
        EXRVersion exr_version;

        ret = ParseEXRVersionFromFile(&exr_version, paths[i]);
        if (ret != 0) {
            fprintf(stderr, "Invalid EXR file: %s\n", paths[i]);
            return -10;
        }

        if (exr_version.multipart) {
            // must be multipart flag is true.
            return -20;
        }

        // 2. Read EXR header
        InitEXRHeader(exr_headers[i]);

        ret = ParseEXRHeaderFromFile(exr_headers[i], &exr_version, paths[i], &err);
        if (ret != 0) {
            fprintf(stderr, "Parse EXR err: %s\n", err);
            FreeEXRErrorMessage(err); // free's buffer for an error message
            return ret;
        }

        InitEXRImage(&exr_images[i]);

        ret = LoadEXRImageFromFile(&exr_images[i], exr_headers[i], paths[i], &err);
        if (ret != 0) {
            fprintf(stderr, "Load EXR err: %s\n", err);
            FreeEXRHeader(exr_headers[i]);
            FreeEXRErrorMessage(err); // free's buffer for an error message
            return ret;
        }

        images[i] = &exr_images[i];
    }
    
    ret = SaveEXRMultipartImageToFile(exr_images, exr_headers, length, targetPath, &err);
    if (ret != 0) {
        fprintf(stderr, "Load EXR err: %s\n", err);
        FreeEXRErrorMessage(err); // free's buffer for an error message
        return ret;
    }

    return 1;
}

DECODER void ReleaseHeader(EXRHeader* header)
{
    FreeEXRHeader(header);
}

DECODER void ReleaseImage(EXRImage* image)
{
    FreeEXRImage(image);
}
