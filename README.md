# What is this?
A testing ground for various niche rendering techniques and approaches. Mostly focussed on unity and the idea of precomputing certain aspects of the rendering pipeline. Not well organized and without a clear roadmap. Just the results of messing around with and learning about computer graphics.

# [Hybrid Rasterizer](https://github.com/wanjawischmeier/pre-rendering/tree/adcfa5ddda5241009e47674efa02a5fcc1522af2/Assets/DataSetFiltering/QuadFilteringDemo)
Reprojection of high-res depth maps is a very computationally intense task. But a hybrid rasterizer (a tile binned software rasterizer for regions of small tris and the hardware rasterization pipeline for the rest) might be able to provide a fast and efficient way to archieve this. The basic flow of the compute shader is as follows:

## Basic flow
1. Pass (gets dispatched with one thread per pixel in input texture)
   - Sample the depth texture for each pixel
   - Compute and transform the respective 3d vetex position
   - Store it in lookup texture (shown as "Vertex Buffer" in example images)
2. Pass (gets dispatched with one thread per entry in vertex buffer)
   - Each point in the vertex buffer is responsible for handling the quad that it shares with its bottom, right and bottom right neighbors. This ensures complete coverage of the surface.
   - A thread samples the 4 vertices that create the quad
       - Backfacing quads get culled here
       - Degenerate quads get filled with a single InterlockedMin operation on the target texture
   - Quad type gets computed (allows for some optimizations if none or only on tri of the quad need(s) to be rasterized)
   - The target texture is covered by 2 grids that are offset by half the tile size. A method checks if the quad fits into a cell of either of those grids.
       - If it fits into Grid A or Grid B, it is small enough to get tile binned and therefore efficiently software rasterized using the next compute shader pass. The quad gets stored in the respective tile buffer (before that, a packed aabb bounding box gets calculated and stored alongside).
       - Otherwise, the quad gets pushed to an AppendStructuredBuffer (has support for atomic operations) for rasterization using unitys standard rendering pipeline.
3. Pass (one dispatch per pixel in output texture)
   - This thread just has to iterate over the all quads in the tile it's contained in (just the aabb's could be loaded into groupshared memory in the future, one by each thread).
   - For each quad, it first checks if it contains the point with an extremely fast check of the quad's aabb. The actual vertices only need to get sampled from the vertex buffer if a quad passes that check.
   - If the actual quad vertices also contain the texel, an InterlockMin write to the output texture is performed (could be done within a tile's groupshared memory in the future to reduce writes operations on the global target texture to a single non-atomic write).
4. Final post processing shader (responsible for combining the hardware and software rasterizer output).

## Memory layout
The **hardware vertex buffer** is comprized of simple uints, where each one holds the quad type (which partial triangles are valid) in the last 2 bits and the index in the vertex buffer in the remaining bits. The **software vertex buffer** packs tile index, vertex buffer index, quad type and aabb into a uint3. The uint **tile counters** are responsible for keeping track of the number of quads being binned into each tile (using a simple InterlockedAdd).

## Images
<img height="400" alt="Screenshot 2025-10-09 202712" src="https://github.com/user-attachments/assets/aa20085a-851b-4377-a443-34305f214f9f" />
<p align="center">
  <img src="https://github.com/user-attachments/assets/1b441819-7db2-4c19-bcc7-74d4d1cbf7f2" height="250">
  <img src="https://github.com/user-attachments/assets/9682b6bb-e6cc-4e84-969d-1cc7d3efdbce" height="250">
</p>


# [C++ Video decoder](https://github.com/wanjawischmeier/pre-rendering/tree/6fc489015d1e897872070886efa8850eea368496/src/video-decoder)
A simple video decoder using OpenCV in C++. The idea was to create a decoder that can be integrated asynchronously into a potential unity render pipeline. That can run in another thread and pass decoded data to a shader running within the unity environment with minimal overhead. This actually ended up working pretty well by utilizing the following structure:
- A C++ DLL that holds the OpenCV instance, implements some callbacks and provides the necessary wrapper functionality
- A C# script in unity that uses an atomic safety handle and a native array to directly pass the frame data that was decoded by the DLL to the shader without the need for a single copy operation on the CPU side
- A HLSL shader that is able to read the frame buffer provided by OpenCV and render the decoded image based on that

The C++ DLL exposes the following methods and callbacks:
```C++
struct VideoInfo
{
	int width, height, fps;
	size_t frame_count;
};

FrameCallback frame_ready;
ErrorMessage error_callback;
VideoInfo video_info;

extern "C" DECODER uchar** InitializeDecoder(
	char* videoPath, int threads,
	FrameCallback frameCallback, ErrorMessage errorCallback,
	VideoInfo &rInfo);
extern "C" DECODER size_t CurrentFrame(int threadIdx);
extern "C" DECODER bool Seek(size_t frameIdx, int threadIdx);
extern "C" DECODER bool Read(size_t frameIdx, int threadIdx);
extern "C" DECODER bool ReadImage(char* path, int threadIdx);
extern "C" DECODER void ReleaseDecoder();
```

Getting the shader right was actually quite tricky, here are some of the iterations it took (more can be found [here](https://mega.nz/folder/ax8WESyL#E4aLQMGvk31w2Lp0UVP-Jg)):
<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/5dca373b-0a59-4755-b163-7ade739ccf0a" alt="chunk_outimg3" height="150"></td>
    <td><img src="https://github.com/user-attachments/assets/bebc0a7c-e203-4c00-b654-627491398ee2" alt="chunk_outimg5" height="150"></td>
    <td><img src="https://github.com/user-attachments/assets/819465eb-e36d-4d99-8631-13977e8d1d81" alt="chunk_outimg7" height="150"></td>
  </tr>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/d33810ab-c36e-49e5-844d-c9223bb8a6c9" alt="outimg" height="150"></td>
    <td><img src="https://github.com/user-attachments/assets/50700ce2-0656-4ed8-9181-365b1d30ee36" alt="outimg3" height="150"></td>
    <td><img src="https://github.com/user-attachments/assets/71cc343b-c0b8-43d7-904c-a734840a487c" alt="outimg5" height="150"></td>
  </tr>
</table>

### Optimal chunk width and decoding times
The ideal size of a chunk is heavily dependent on seek and frame times (frame time = time to decode one frame). Below is a look at how the optimal chunk size changes with different resolutions and decoding stats:

<p align="center">
  <img src="https://github.com/user-attachments/assets/eec84842-9ec7-4615-ab9f-302a5607764f" height="250">
  <img src="https://github.com/user-attachments/assets/791df18a-8507-4de1-bc10-3b4c7f6e5865" height="250">
</p>


### Installation and Setup
<details>
	<summary>Setting up the build environment</summary>
	
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
</details>

<br>

# [Downhill simplex approximation of a projection](https://github.com/wanjawischmeier/pre-rendering/tree/6fc489015d1e897872070886efa8850eea368496/src/unity/concept/Assets/DownhillSimplexAbstract)
So in retrospect i don't really see why I thought this could work (the algorithm just ended up not nearly converging fast enough), but it still made for a really interesting experiment. Especially the visuals were stunning. More images can be found [here](https://mega.nz/folder/G9lE0QIY#6BS4I_LkFfoOj8BjSzC4Ag).

<details>
  <summary>The basic downhill simplex algorithm used</summary>

```hlsl
float2 downhillSimplex(float2 x0, float2 x1, float2 x2) {
  // initialization
  float3 b = float3(x0, objective(x0));
  float3 g = float3(x1, objective(x1));
  float3 w = float3(x2, objective(x2));

  [unroll(ITERATIONS)] for (int i = 0; i < ITERATIONS; i++) {
    // sort
    float3 t;

    if (b.z > g.z) {
      t = g;
      g = b;
      b = t;
    }

    if (g.z > w.z) {
      t = g;
      g = w;
      w = t;

      if (b.z > g.z) {
        t = g;
        g = b;
        b = t;
      }
    }

    // midpoint
    float3 m;
    m.xy = (g + b) / 2;

    // reflection
    float3 r;
    r.xy = m.xy + ALPHA * (m.xy - w.xy);
    r.z = objective(r.xy);

    if (r.z < g.z)
      w = r;

    else {
      if (r.z < w.z) w = r;

      float3 h;
      h.xy = (w.xy + m.xy) / 2.0;  // try int 2
      h.z = objective(h.xy);

      if (h.z < w.z) w = h;
    }

    // expansion
    if (r.z < b.z) {
      float3 e;
      e.xy = m.xy + GAMMA * (r.xy - m.xy);
      e.z = objective(e.xy);

      if (e.z < r.z)
        w = e;

      else
        w = r;
    }

    // contraction
    if (r.z > g.z) {
      float3 c;
      c.xy = m.xy + BETA * (w.xy - m.xy);
      c.z = objective(c.xy);

      if (c.z < w.z) w = c;
    }
  }

  return b.xy;
}

fixed4 frag(v2f i) : SV_Target {
  // fixed4 col = tex2D(_MainTex, i.uv);
  float err = objective(i.uv);
  float2 opt = downhillSimplex(i.uv, X1, X2);

  fixed4 col = fixed4(opt.xy * FAC + OFF, tan(1 - opt.x), 1);
  return col;
}
```
More details can be found [here](https://github.com/wanjawischmeier/pre-rendering/tree/67f97403e655963fdde4d20cd524f7da77558d9c/src/unity/concept/Assets/DownhillSimplexAbstract).
</details>

<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/c7dc2ec5-3188-4183-9399-16e8f32d9a30" alt="downhill_simplex_lowd2" height="150"></td>
    <td><img src="https://github.com/user-attachments/assets/7042687c-0f75-4193-b8bc-0bf2dce17815" alt="downhill_simplex_abstract7" height="150"></td>
    <td><img src="https://github.com/user-attachments/assets/9ae7f4dc-cffa-4855-b9b0-3120187677b4" alt="downhill_simplex_abstract6" height="150"></td>
  </tr>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/8e67eeb8-45df-49ff-9581-324d3e7e3b7f" alt="downhill_simplex_abstract5" height="150"></td>
    <td><img src="https://github.com/user-attachments/assets/59f480da-54d7-4d26-bf04-453c67a79451" alt="downhill_simplex_abstract3" height="150"></td>
    <td><img src="https://github.com/user-attachments/assets/e9019740-abec-48ad-a35d-dfa6c0208c00" alt="downhill_simplex_abstract4" height="150"></td>
  </tr>
</table>

<br>

# Camera robot
A way to generate panorama images in a scanline path (as the [blender plugin](#blender-plugin) does virtually) in the real world would be nice. I tried building a Lego Mindstorms robot that is able to have a 2 axis camera arm and still drive around using tank steering. But I only had 3 motors. Eventually got it working using a ratcheting mechanism, but it was pretty janky and wobbly. 

<img src="https://github.com/user-attachments/assets/71352b08-d8a9-4d1f-bc65-a334cbb41fc2" alt="output" style="height:250px; display:inline-block;">
<img src="https://github.com/user-attachments/assets/0a12bb30-4a9c-4650-a694-09bc81ae93f2" alt="PXL_20210723_083920526" style="height:250px; display:inline-block;">

<br>

# [Blender plugin](https://github.com/wanjawischmeier/pre-rendering/tree/6fc489015d1e897872070886efa8850eea368496/src/blender-plugin)
A blender plugin that allows you to
- Dynamically create scanline paths for a camera to take
- Set up a node network for the camera to render with equirectangular projection
- Creates a compositor group that allows users to generate "map" files from a video render
- Write a config file to be read as part of that map by a unity loader script

Builds for various iterations of this plugin can be found [here](https://mega.nz/folder/rxkiCaha#VUtcXZtMB2u_PibZzXbfew).

<img width="353" height="250" alt="grafik" src="https://github.com/user-attachments/assets/c331e53a-181a-4a23-b18b-a0962b587825" />

<br>

# [Image channel combining](https://github.com/wanjawischmeier/pre-rendering/blob/6fc489015d1e897872070886efa8850eea368496/src/python-testing/image_packing_demo.py)
I wanted to be able to encode the depth information required for reprojection in video files for quick hardware accelerated decoding. But those formats were often limited to a percision of 8 bits, which is insufficient for depth information. So I experimented splitting 16 bit depth information into two 8 bit channels (and later recombining them).

<img src="https://github.com/user-attachments/assets/f35cd58a-b247-4041-ad08-914aab649256" alt="v1" style="height:250px; display:inline-block;">
<img src="https://github.com/user-attachments/assets/d17f0df0-b628-4438-a47c-22c63ca40db0" alt="v2" style="height:250px; display:inline-block;">

<br>

# Storing alternative color channels
The idea was to maybe not store diffuse color as is usually the case. But rather raw properties and then apply them dynamically in the rendering pipeline. This never went very far though.

| diffuse | emision | intensity | inverted |
|---------|---------|-----------|----------|
| ![diffuse](https://github.com/user-attachments/assets/9f7c53ce-e236-4635-b92d-eb51eda27980) | ![emision](https://github.com/user-attachments/assets/f265fa61-621e-4a3b-934c-15719721d4fe) | ![intensity](https://github.com/user-attachments/assets/c7b90b1b-a483-4825-b6d0-de84651127ef) | ![inverted](https://github.com/user-attachments/assets/b1dd536e-ac8b-49f6-89ea-d02d9ce1c511) |

<br>

# Codec testing
Losless storage isn't viable in most cases, so depth information stored in images will also be subjected to compression artifacts. I tried to take a look at how different file formats compress depth and normal information (particularly at the edges). The files can be found [here](https://mega.nz/folder/7o1CjKbS#mbZdCKbwdRgMa86xMrvJoA).

<img width="1548" height="548" alt="Image" src="https://github.com/user-attachments/assets/ec9f9f6f-63b6-4ba2-9951-7b9bc1d7f6b2" />

# More data
More extensive data from various tests (about 10GB as of now) can be found [here](https://mega.nz/folder/GpNBXDaB#RRB_icj2zn0b7b5NIU4keQ). Feel free to check it out (the `Images` folder has lots of interesting screenshots from the different experiments).


# Credit
https://github.com/bodhid/UnityEquiCam

@inproceedings{zhang2018single,
  title = {Single Image Reflection Separation with Perceptual Losses},
  author = {Zhang, Xuaner and Ng, Ren and Chen, Qifeng}
  booktitle = {IEEE Conference on Computer Vision and Pattern Recognition},
  year = {2018}
}
