
#if defined(TILE_SIZE_4)
#define TILE_SIZE 4
    // Multiplier must be coprime with TILE_SIZE * TILE_SIZE (i.e., 256 if TILE_SIZE = 16)
    // or in other words, gcd(COPRIME_MULTIPLIER, sq(TILE_SIZE)) needs to be 1
#define COPRIME_MULTIPLIER 3
#elif defined(TILE_SIZE_8)
#define TILE_SIZE 8
#define COPRIME_MULTIPLIER 11
#elif defined(TILE_SIZE_16)
#define TILE_SIZE 16
#define COPRIME_MULTIPLIER 109
#elif defined(TILE_SIZE_32)
#define TILE_SIZE 32
#define COPRIME_MULTIPLIER 311
#else
#define TILE_SIZE 8 // Default value
#define COPRIME_MULTIPLIER 11
#endif

#if defined(TILE_CAPACITY_2)
#define MAX_VALID_TEXELS 2
#elif defined(TILE_CAPACITY_4)
#define MAX_VALID_TEXELS 4
#elif defined(TILE_CAPACITY_8)
#define MAX_VALID_TEXELS 8
#elif defined(TILE_CAPACITY_16)
#define MAX_VALID_TEXELS 16
#else
#define MAX_VALID_TEXELS 4 // Default value
#endif

#define VALIDITY_DETECTION_THRESHOLD 0.05

struct Tile
{
    float2 uvs[MAX_VALID_TEXELS];
    uint pointCount;
};
