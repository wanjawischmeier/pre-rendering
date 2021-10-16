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
    [RequireComponent(typeof(MovementController))]
    public class PreRenderer : MonoBehaviour
    {
        [Header("Map")]
        public string mapName;
        string mapPath;

        [Header("Decoder")]
        [Range(1, 100)]
        public int cacheSize = 10;
        [Range(1, 10)]
        public int decodingThreads = 4;

        [Header("Projection")]
        public ComputeShader projectShader;
        const float minimumPercision = 0.1f;
        const float maximumPercision = 1;
        [Range(minimumPercision, maximumPercision)]
        public float geometryPercision = 0.75f;
        [Range(1, 5)]
        public float falloff = 2;
        [Range(1, 100)]
        public int layerDepth = 4;

        [Header("Post Processing")]
        public Shader postProcessing;
        public bool debug = false;
        [Range(0, 0.5f)]
        public float cutoff = 0.5f;

        [HideInInspector]
        public MovementController controller;
        [HideInInspector]
        public Resolution projectionResolution;

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

        public Map map;
        TextureBuffer buffer;
        DecodingThread decoder;
        ShaderManager shaderManager;

        void Start()
        {
            string mainPath = Application.dataPath.Split(new string[] { "pre-rendering" }, System.StringSplitOptions.None)[0];
            mainPath = Path.Combine(mainPath, "pre-rendering/master/renders");

            controller = GetComponent<MovementController>();
            projectionResolution = Utility.EstimatePanoramaResolution(
                Mathf.RoundToInt(Screen.width * geometryPercision),
                Mathf.RoundToInt(Screen.height * geometryPercision),
                Camera.main.fieldOfView);

            mapPath = Path.Combine(mainPath, mapName);
            map = new Map(mapPath);
            buffer = new TextureBuffer(map.textureWidth, map.textureHeight, cacheSize);
            decoder = new DecodingThread(buffer, decodingThreads);
            shaderManager = new ShaderManager(
                projectShader, postProcessing, buffer.textures,
                projectionResolution, map, cacheSize);

#if UNITY_EDITOR && HEAVY_DEBUG
            arrayDebug = new Texture2DArray(map.textureWidth, map.textureHeight, cacheSize, TextureFormat.RGBA32, 1, false);
#endif
        }

        void Update()
        {
            shaderManager.position = transform.position;
            shaderManager.rotation = transform.eulerAngles;
            shaderManager.fov = Camera.main.fieldOfView;
            shaderManager.debug = debug;
            shaderManager.cutoff = cutoff;

            Vector3[] positions = map.vectorOffsets.GetClosest(transform.position, cacheSize);
            List<Vector3> availablePositions = new List<Vector3>();

            // Load the closest images into the buffer
            for (int i = 0; i < cacheSize; i++)
            {
                Vector3 positionOffset = positions[i];
                string path = map.vectorOffsets.GetFileName(mapPath, positionOffset);

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
            foreach (Vector3 vector in reserved)
                if (!positions.Contains(vector)) buffer.Release(vector);

#if UNITY_EDITOR && HEAVY_DEBUG
            pendingDebug = new Vector3[decoder.pending.Count];
            decodingDebug = new Vector3[decoder.decoding.Count];
            reservedDebug = new VectorIndex[buffer.reserved.Count];
            availableDebug = new int[buffer.available.Count];

            int idx = 0;
            foreach (KeyValuePair<string, Vector3> item in decoder.pending)
                pendingDebug[idx++] = decoder.pending[item.Key];

            idx = 0;
            foreach (KeyValuePair<AsyncOperation, Tuple<Vector3, UnityWebRequest>> item in decoder.decoding)
                decodingDebug[idx++] = item.Value.Item1;

            idx = 0;
            foreach (KeyValuePair<Vector3, int> item in buffer.reserved)
                reservedDebug[idx++] = new VectorIndex { vector = item.Key, index = item.Value };

            idx = 0;
            foreach (int item in buffer.available)
                availableDebug[idx++] = item;

            Graphics.CopyTexture(buffer.textures, arrayDebug);
#endif
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination) =>
            shaderManager.Render(ref destination);

        void OnDestroy()
        {
            buffer.Release();
            shaderManager.Release();
        }
    }
}