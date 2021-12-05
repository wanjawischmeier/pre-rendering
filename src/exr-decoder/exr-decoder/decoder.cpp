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

    // 2. Read EXR headers in the EXR.
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
