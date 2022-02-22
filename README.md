# Setting up the build environment

## Video Decoder

1. Download and install the [OpenCV binaries](https://opencv.org/releases) (tested with v4.5.5)

2. Download and install [Visual Studio](https://visualstudio.microsoft.com/de/downloads) (tested with Visual Studio 2022 Community)

    * Make sure to include the *`Desktop Development with C++`* workload when running the installer
<br><br>

3. Open the *`pre-rendering/src/video-decoder/video-decoder.sln`* solution in Visual Studio

4. Go to *`View > Other Windows > Property Manager`*

5. Expand any configuration and open the *`LibraryPaths`* Property Sheet

6. Go to *`Common Properties > User Macros`*

7. Enter the path of your OpenCV installation as the value for the *`OpenCV`* macro (e.g. *`C:/libraries/opencv`*)

8. Hit *`OK`*, *`Apply`*, and then *`OK`* again

9. Open a terminal inside the repo and run the following command

    ```git update-index --assume-unchanged .\src\video-decoder\LibraryPaths.props```

10. Add the path of your OpenCV binaries as an enviromnment variable (e.g. *`C:/libraries/opencv/build/x64/vc15/bin`*) 

The installation process should be complete now. Try building the *`video-decoder`* solution by pressing *`STRG`* + *`B`*




(*`Game Development with Unity`*)

# Credit
https://github.com/bodhid/UnityEquiCam

@inproceedings{zhang2018single,
  title = {Single Image Reflection Separation with Perceptual Losses},
  author = {Zhang, Xuaner and Ng, Ren and Chen, Qifeng}
  booktitle = {IEEE Conference on Computer Vision and Pattern Recognition},
  year = {2018}
}
