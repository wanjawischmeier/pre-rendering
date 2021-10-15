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
        public ComputeShader projectShader;
        public Shader postProcessing;
        [HideInInspector]
        public MovementController controller;

        public string mainPath;
        public string mapName;
        string mapPath;
        public Vector2Int geometryResolution;
        [Range(1, 100)]
        public int layerDepth = 4;
        [Range(1, 100)]
        public int cacheSize = 10;
        [Range(1, 10)]
        public int decodingThreads;
        public bool debug;
        [HideInInspector]
        public int selectedId = 1;

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
        
        public List<Vector3> positions;
        // Vector3[] debugOffArray;
#else
        List<Vector3> positions;
        // Vector3[] debugOffArray;
#endif

        public Map map;
        TextureBuffer buffer;
        DecodingThread decoder;
        ShaderManager shaderManager;

        void Start()
        {
            controller = GetComponent<MovementController>();
            
            mapPath = Path.Combine(mainPath, mapName);
            map = new Map(mapPath);
            buffer = new TextureBuffer(map.textureWidth, map.textureHeight, cacheSize);
            decoder = new DecodingThread(buffer, decodingThreads);
            shaderManager = new ShaderManager(
                projectShader, postProcessing, buffer.textures,
                Screen.currentResolution, geometryResolution, map,
                layerDepth, cacheSize);

#if UNITY_EDITOR && HEAVY_DEBUG
            arrayDebug = new Texture2DArray(map.textureWidth, map.textureHeight, cacheSize, TextureFormat.RGBA32, 1, false);
#else
            positions = new List<Vector3>();
#endif
        }

        void Update()
        {
            shaderManager.position = transform.position;
            shaderManager.rotation = transform.eulerAngles;
            shaderManager.fov = Camera.main.fieldOfView;
            shaderManager.debug = debug;

            Vector3[] newPositions = map.vectorOffsets.GetClosest(transform.position, cacheSize);
            positions.Clear();

            // Load the closest images into the buffer
            for (int i = cacheSize -1, j = 0; i >= 0; i--)
            {
                Vector3 positionOffset = newPositions[i];
                string path = map.vectorOffsets.GetFileName(mapPath, positionOffset);

                if (buffer.Contains(positionOffset))
                {
                    if (j++ < layerDepth)
                    {
                        float distance = Vector3.Distance(transform.position, positionOffset);
                        // TODO: Resolution based on distance
                        int projectWidth = geometryResolution.x;
                        int projectHeight = geometryResolution.y;

                        shaderManager.positionOffset = positionOffset;
                        shaderManager.Project(projectWidth, projectHeight, buffer[positionOffset]);
                    }
                }
                else
                    decoder.DecodeToBuffer(path, positionOffset);
            }

            // Release all free positions
            Vector3[] reserved = buffer.reserved.Keys.ToArray();
            foreach (Vector3 vector in reserved)
                if (!newPositions.Contains(vector)) buffer.Release(vector);

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

            // debugOffArray[selectedId - 1] = controller.secondaryPosition;
            // shaderManager.debugPositionArray = debugOffArray;
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