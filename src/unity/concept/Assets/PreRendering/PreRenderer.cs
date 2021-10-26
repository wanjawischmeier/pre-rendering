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
#if UNITY_EDITOR
        public bool[] editorAreas = new bool[4];
#endif

        // Map
        public string renderPath;
        public string mapName;
        string mapPath;

        // Decoder
        public int cacheSize = 10;
        public int decodingThreads = 4;

        // Projection
        public ComputeShader projectShader;
        public float geometryPercision = 0.75f;

        // Post Processing
        public ShaderManager.ShaderDebugMode shaderDebug = ShaderManager.ShaderDebugMode.Disabled;
        public float depthOfField = 0;
        public float mistOffset = 1;
        public float mistFalloff = 0.1f;
        public Color mist = Color.white;


        public MovementController controller;
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

            controller = GetComponent<MovementController>();
            
            mapPath = Path.Combine(renderPath, mapName);
            map = new Map(mapPath);

            projectionResolution = map.resolution.Multiply(geometryPercision);
            screenResolution = map.resolution.EstimateScreenResolution(mainCamera.fieldOfView);

            buffer = new TextureBuffer(map.resolution.width, map.resolution.height, cacheSize);
            decoder = new DecodingThread(buffer, decodingThreads);
            shaderManager = new ShaderManager(
                projectShader, buffer.textures,
                projectionResolution, map, cacheSize);

#if UNITY_EDITOR
#if HEAVY_DEBUG
            arrayDebug = new Texture2DArray(map.textureWidth, map.textureHeight, cacheSize, TextureFormat.RGBA32, 1, false);
#endif
#else
            Screen.SetResolution(screenResolution.width, screenResolution.height, true);
#endif
        }

        void Update()
        {
            shaderManager.Position = transform.position;
            shaderManager.Rotation = transform.eulerAngles;
            shaderManager.Fov = mainCamera.fieldOfView;
            shaderManager.DOFIntensity = depthOfField;
            shaderManager.ShaderDebug = shaderDebug;
            shaderManager.Mist = mist;
            shaderManager.MistFalloff = mistFalloff;
            shaderManager.MistOffset = mistOffset;

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

            if (availablePositions.Count > 0)
            {
                Vector3 positionOffset = availablePositions[0];

                shaderManager.PositionOffset = positionOffset;
                shaderManager.Project(
                    Mathf.RoundToInt(projectionResolution.width),
                    Mathf.RoundToInt(projectionResolution.height),
                    buffer[positionOffset]);

                // Release all free positions
                Vector3[] reserved = buffer.reserved.Keys.ToArray();
                foreach (var vector in reserved)
                    if (!positions.Contains(vector)) buffer.Release(vector);
            }

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