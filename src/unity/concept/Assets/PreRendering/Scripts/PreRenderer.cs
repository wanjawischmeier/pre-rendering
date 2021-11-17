using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace PreRendering
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MovementController))]
    [HelpURL("https://github.com/wanjawischmeier/pre-rendering/")]
    public class PreRenderer : MonoBehaviour
    {
#if UNITY_EDITOR
        public bool[] foldouts;
        public string[] foldoutNames =
        {
            "Map",
            "Decoder",
            "Projection & Post Processing",
            "Debugging"
        };
#endif

        // Map
        public string renderPath;
        public string[] mapPaths;
        public string[] mapFiles;
        public int mapSelection;
        private string mapPath;

        // Decoder
        public float predictionBlend = 0.75f;
        public float predictionDistance = 2;
        public int cacheSize = 10;
        public ThreadPriority decodingPriority = ThreadPriority.BelowNormal;
        public int decodingPrioritySelection;
        public int decodingThreads = 4;

        // Projection & Post Processing
        public float geometryPercision = 0.75f;
        public ShaderManager.ShaderDebugMode shaderDebug = ShaderManager.ShaderDebugMode.Disabled;
        public int shaderDebugSelection;
        public float depthOfField = 0;
        public float mistOffset = 1;
        public float mistFalloff = 0.1f;
        public Color mist = Color.white;

        // Debugging
        public int[] debuggingInts;
        public string[] debuggingIntNames =
        {
            "FIXED_IDX"
        };
        
        public MovementController controller;
        public Resolution projectionResolution;
        public Resolution screenResolution;
        private Vector3 positionOffset = default;
        private Vector3 lastPosition = default;

        public Map map;
        private RawTexture.Buffer buffer;
        private DecodingManager decoder;
        private ShaderManager shaderManager;
        private Camera mainCamera;

        public int pending, decoding;
        public const string RepoName = "pre-rendering";


        private void Start()
        {
#if UNITY_EDITOR
            string rootPath = Application.dataPath.Split(new string[] { "pre-rendering" }, System.StringSplitOptions.None)[0];
            renderPath = Path.Combine(rootPath, RepoName, "renders");

            mapPath = Path.Combine(renderPath, mapPaths[mapSelection]);
#else
            renderPath = Application.dataPath;

            string mapFile = Directory.GetFiles(renderPath, ".mapconfig", SearchOption.AllDirectories)[0];
            mapPath = Path.GetDirectoryName(mapFile);
#endif
            map = new Map(mapPath);

            mainCamera = Camera.main;
            controller = GetComponent<MovementController>();


            projectionResolution = map.resolution.Multiply(geometryPercision);
            screenResolution = map.resolution.EstimateScreenResolution(mainCamera.fieldOfView);

#if !UNITY_EDITOR
            Screen.SetResolution(screenResolution.width, screenResolution.height, true);
#endif

            Decoder.Initialize(map.GetFileName(Vector3.zero), cacheSize);
            buffer = new RawTexture.Buffer(Decoder.bufferPointer, map.resolution.width, map.resolution.height, cacheSize);
            decoder = new DecodingManager(buffer, decodingThreads, cacheSize);
            shaderManager = new ShaderManager();

            ShaderManager.SetValues(
                new ShaderManager.Property()
                {
                    name = "MX_IDX",
                    value = cacheSize
                },
                new ShaderManager.Property()
                {
                    name = "PI",
                    value = Mathf.PI
                },
                new ShaderManager.Property()
                {
                    name = "PI2",
                    value = Mathf.PI * 2
                },
                new ShaderManager.Property()
                {
                    name = "NCLIP",
                    value = map.nClip
                },
                new ShaderManager.Property()
                {
                    name = "FCLIP",
                    value = map.fClip
                },
                new ShaderManager.Property()
                {
                    name = "InputBufferResolution",
                    value = map.resolution.ToVector()
                },
                new ShaderManager.Property()
                {
                    name = "ProjectedResolution",
                    value = projectionResolution.ToVector()
                },
                new ShaderManager.Property()
                {
                    name = "InputBuffer",
                    value = buffer.computeBuffer
                });
        }

        private void Update()
        {
#if !UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
#endif
            decoder.priority = decodingPriority;

            // Set shader values
            ShaderManager.SetValues(
                new ShaderManager.Property()
                {
                    name = "FOV",
                    value = mainCamera.fieldOfView * Mathf.Deg2Rad,
                    material = shaderManager.postProcessingMaterial
                },
                new ShaderManager.Property()
                {
                    name = "Debug",
                    value = shaderDebugSelection,
                    material = shaderManager.postProcessingMaterial
                },
                new ShaderManager.Property()
                {
                    name = "DOF_INTENSITY",
                    value = depthOfField,
                    material = shaderManager.postProcessingMaterial
                },
                new ShaderManager.Property()
                {
                    name = "MIST_FALLOFF",
                    value = mistFalloff,
                    material = shaderManager.postProcessingMaterial
                },
                new ShaderManager.Property()
                {
                    name = "MIST_OFFSET",
                    value = mistOffset,
                    material = shaderManager.postProcessingMaterial
                },
                new ShaderManager.Property()
                {
                    name = "MIST_COL",
                    value = (Vector4)mist,
                    material = shaderManager.postProcessingMaterial
                },
                new ShaderManager.Property()
                {
                    name = "Position",
                    value = (Vector4)transform.position,
                    material = shaderManager.projectionMaterial
                },
                new ShaderManager.Property()
                {
                    name = "Rotation",
                    value = (Vector4)transform.eulerAngles * Mathf.Deg2Rad,
                    material = shaderManager.postProcessingMaterial
                });
            

            // Set debug values
            for (int i = 0; i < debuggingInts.Length; i++)
                Shader.SetGlobalInt(debuggingIntNames[i], debuggingInts[i]);

            Vector3[] positions = map.offsets.GetClosest(transform.position, cacheSize + 1);
            // Vector3[] positions = map.offsets.PredictClosest(lastPosition, transform.position, cacheSize, predictionBlend, predictionDistance);
            Vector3 temp;

            // Clear old pending positions if the player has moved
            if (transform.position != lastPosition) decoder.ClearPending();
            lastPosition = transform.position;

            // Load the closest image synchronously if it isn't available yet
            if (!buffer.ContainsAny(positions))
            {
                temp = positions[0];
                string path = map.GetFileName(temp);
                decoder.DecodeToBuffer(path, temp);
                positionOffset = temp;
            }

            // Load the closest images into the buffer asynchronously
            for (int i = cacheSize; i >= 0; i--)
            {
                temp = positions[i];
                string path = map.GetFileName(temp);

                if (buffer.Contains(temp))
                    positionOffset = temp;
                else
                    decoder.DecodeToBufferAsync(path, temp);
            }

            // Project
            buffer.Refresh();

            ShaderManager.SetValues(new ShaderManager.Property()
            {
                name = "PositionOffset",
                value = (Vector4)positionOffset,
                material = shaderManager.projectionMaterial
            });

            shaderManager.Project(
                Mathf.RoundToInt(projectionResolution.width),
                Mathf.RoundToInt(projectionResolution.height),
                buffer[positionOffset]);

            // Release all free positions
            Vector3[] reserved = buffer.reserved.Keys.ToArray();
            foreach (var vector in reserved)
                if (!positions.Contains(vector)) buffer.Release(vector);

            // Debugging
            pending = decoder.Pending;
            decoding = decoder.Decoding;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination) =>
            shaderManager.Render(ref destination);

        private void OnDestroy()
        {
            decoder.Release();
            buffer.Release();
            shaderManager.Release();
        }
    }

    public static partial class Utility
    {
        /// <summary>
        /// Estimates the resolution a panorama projected using gnomonic projection will have.
        /// </summary>
        public static Resolution EstimateScreenResolution(this Resolution resolution, float fov)
        {
            return EstimateScreenResolution(resolution.width, resolution.height, fov);
        }

        public static Resolution EstimateScreenResolution(int width, int height, float fov)
        {
            var res = new Resolution
            {
                width = Mathf.RoundToInt(width * fov / 360),
                height = Mathf.RoundToInt(height * fov / 180)
            };
            return res;
        }

        /// <summary>
        /// Get the vectors that have the smallest distance to the specified target position.
        /// These vectors originate from the 'position' vector and are ordered in an outwards spiraling pattern.
        /// </summary>
        /// <param name="amount">The desired length of the returned array.</param>
        public static Vector3[] GetClosest(this Vector3[] vectors, Vector3 position, int amount)
        {
            return vectors
                .OrderBy(x => Vector3.Distance(position, x))
                .Take(amount)
                .ToArray();
        }

        /// <summary>
        /// Get the vectors that have the smallest distance to the specified target position.
        /// These vectors originate from the 'position' vector and are ordered in an outwards spiraling pattern.
        /// </summary>
        /// <param name="amount">The desired length of the returned array.</param>
        public static Vector3[] PredictClosest(this Vector3[] vectors, Vector3 oldPosition, Vector3 newPosition, int amount, float blend = 0.5f, float predictionDistance = 2)
        {
            return vectors
                .OrderBy(x =>
                {
                    Vector3 P = oldPosition + predictionDistance * (newPosition - oldPosition);
                    return (1 - blend) * Vector3.Distance(newPosition, x) + blend * Vector3.Distance(P, x);
                })
                .Take(amount)
                .ToArray();
        }

        public static Resolution Multiply(this Resolution resolution, float value)
        {
            return new Resolution()
            {
                width = Mathf.RoundToInt(resolution.width * value),
                height = Mathf.RoundToInt(resolution.height * value)
            };
        }

        public static bool ContainsAny<T>(this IEnumerable<T> enumerable1, IEnumerable<T> enumerable2)
        {
            foreach (var item in enumerable1)
            {
                if (enumerable2.Contains(item)) return true;
            }

            return false;
        }

        public static Vector4 ToVector(this Resolution resolution)
        {
            return new Vector4(resolution.width, resolution.height);
        }
    }
}