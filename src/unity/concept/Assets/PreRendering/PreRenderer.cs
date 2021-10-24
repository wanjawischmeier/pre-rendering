using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR && HEAVY_DEBUG
using System;
using UnityEngine.Networking;
#endif

namespace PreRendering
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MovementController))]
    [HelpURL("https://github.com/wanjawischmeier/pre-rendering/")]
    public class PreRenderer : MonoBehaviour
    {
        [Header("Map")]

        [SerializeField]
        [Tooltip("The folder the map should be contained in (Will be overwritten on start, changing it won't do anything).")]
        string rendersPath;

        [SerializeField]
        [Tooltip("The name of the folder the '.mapconfig' file is contained in. This folder has to be inside the 'renders' parent folder.")]
        string mapName;

        string mapPath;


        [Header("Decoder")]
        
        [SerializeField]
        [Tooltip("The size of the texture cache.")]
        [Range(1, 100)]
        int cacheSize = 10;

        [SerializeField]
        [Tooltip("Maximum amount of textures to be decoded at once.")]
        [Range(1, 10)]
        int decodingThreads = 4;

        [Header("Projection")]

        [SerializeField]
        [Tooltip("The compute shader that countains the kernels needed for projection ('Project' and 'Combine').")]
        ComputeShader projectShader;

        [SerializeField]
        [Tooltip("The base value the screen resolution should be divided by for projection.")]
        [Range(minimumPercision, maximumPercision)]
        float geometryPercision = 0.75f;

        [SerializeField]
        [Tooltip("How fast the resolution decreases in the distance. Smaller values for slower falloff.")]
        [Range(0.1f, 5)]
        float falloff = 2;

        [SerializeField]
        [Tooltip("The amount of textures to be projected.")]
        [Range(1, 20)]
        int layerDepth = 4;


        [Header("Post Processing")]

        [SerializeField]
        [Tooltip("The shader that applies post processing to the projected image (gnomonic projection etc.).")]
        Shader postProcessing;

        [SerializeField]
        [Tooltip("If enabled, the post processing shader will just pass through the projected texture coordinates.")]
        ShaderManager.ShaderDebugMode shaderDebug = ShaderManager.ShaderDebugMode.Disabled;

        [HideInInspector]
        public MovementController controller;
        [HideInInspector]
        public Resolution projectionResolution;
        public Resolution screenResolution;

        public Map map;
        TextureBuffer buffer;
        DecodingThread decoder;
        ShaderManager shaderManager;
        Camera mainCamera;

        const float minimumPercision = 0.1f;
        const float maximumPercision = 1;

#if UNITY_EDITOR && HEAVY_DEBUG
        [Serializable]
        public struct VectorIndex
        {
            public Vector3 vector;
            public int index;
        }

        public Vector3[] pendingDebug;
        public Vector3[] decodingDebug;
        public VectorIndex[] reservedDebug;
        public int[] availableDebug;
        public Texture2DArray arrayDebug;
#endif

        void Start()
        {
            mainCamera = Camera.main;

            rendersPath = Application.dataPath.Split(new string[] { "pre-rendering" }, System.StringSplitOptions.None)[0];
            rendersPath = Path.Combine(rendersPath, "pre-rendering/master/renders");

            controller = GetComponent<MovementController>();

            mapPath = Path.Combine(rendersPath, mapName);
            map = new Map(mapPath);

            projectionResolution = map.resolution.Multiply(geometryPercision);
            screenResolution = map.resolution.EstimateScreenResolution(mainCamera.fieldOfView);

            buffer = new TextureBuffer(map.resolution.width, map.resolution.height, cacheSize);
            decoder = new DecodingThread(buffer, decodingThreads);
            shaderManager = new ShaderManager(
                projectShader, postProcessing, buffer.textures,
                projectionResolution, map, cacheSize);

#if UNITY_EDITOR
#if HEAVY_DEBUG
            arrayDebug = new Texture2DArray(map.textureWidth, map.textureHeight, cacheSize, TextureFormat.RGBA32, 1, false);
#endif
#else
            Screen.SetResolution(screenResolution.width, screenResolution.height, true);
#endif
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            rendersPath = Application.dataPath.Split(new string[] { "pre-rendering" }, System.StringSplitOptions.None)[0];
            rendersPath = Path.Combine(rendersPath, "pre-rendering/master/renders");
        }
#endif

        void Update()
        {
            shaderManager.position = transform.position;
            shaderManager.rotation = transform.eulerAngles;
            shaderManager.fov = mainCamera.fieldOfView;
            shaderManager.shaderDebug = shaderDebug;

            Vector3[] positions = map.offsets.GetClosest(transform.position, cacheSize);
            List<Vector3> availablePositions = new List<Vector3>();

            // Load the closest images into the buffer
            for (int i = 0; i < cacheSize; i++)
            {
                Vector3 positionOffset = positions[i];
                string path = map.offsets.GetFileName(mapPath, positionOffset);

                if (buffer.Contains(positionOffset))
                    availablePositions.Add(positionOffset);
                else
                    decoder.DecodeToBuffer(path, positionOffset);
            }

            for (int i = layerDepth -1; i >= 0; i--)
            {
                if (availablePositions.Count <= i) continue;
                Vector3 positionOffset = availablePositions[i];

                float distance = Vector3.Distance(transform.position, positionOffset);
                float scalar = Mathf.Clamp(distance / falloff,
                    minimumPercision, maximumPercision);

                shaderManager.positionOffset = positionOffset;
                shaderManager.Project(
                    Mathf.RoundToInt(projectionResolution.width * scalar),
                    Mathf.RoundToInt(projectionResolution.height * scalar),
                    buffer[positionOffset]);
            }
            
            // Release all free positions
            Vector3[] reserved = buffer.reserved.Keys.ToArray();
            foreach (var vector in reserved)
                if (!positions.Contains(vector)) buffer.Release(vector);

#if UNITY_EDITOR && HEAVY_DEBUG
            pendingDebug = new Vector3[decoder.pending.Count];
            decodingDebug = new Vector3[decoder.decoding.Count];
            reservedDebug = new VectorIndex[buffer.reserved.Count];
            availableDebug = new int[buffer.available.Count];

            int idx = 0;
            foreach (var item in decoder.pending)
                pendingDebug[idx++] = decoder.pending[item.Key];

            idx = 0;
            foreach (var item in decoder.decoding)
                decodingDebug[idx++] = item.Value.Item1;

            idx = 0;
            foreach (var item in buffer.reserved)
                reservedDebug[idx++] = new VectorIndex { vector = item.Key, index = item.Value };

            idx = 0;
            foreach (var item in buffer.available)
                availableDebug[idx++] = item;

            Graphics.CopyTexture(buffer.textures, arrayDebug);
#endif
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination) =>
            shaderManager.Render(ref destination);

        void OnDestroy()
        {
            buffer.Release();
            decoder.Release();
            shaderManager.Release();
        }
    }
}