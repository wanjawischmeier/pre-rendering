using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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

        public Vector3[] positionArray;
        Vector3[] debugOffArray;
        public int selectedId = 1;

#if UNITY_EDITOR
        [Serializable]
        public struct VectorIndex
        {
            public Vector3 vector;
            public int index;
        }

        public Vector3[] pending;
        public Vector3[] decoding;
        public VectorIndex[] reserved;
        public int[] available;
        public Texture2DArray array;
#endif

        public Map map;
        FrameBuffer buffer;
        Decoder decoder;
        ShaderManager shaderManager;

        void Start()
        {
            controller = GetComponent<MovementController>();
            
            mapPath = Path.Combine(mainPath, mapName);
            map = new Map(mapPath);
            buffer = new FrameBuffer(map.textureWidth, map.textureHeight, cacheSize);
            decoder = new Decoder(buffer, decodingThreads);
            shaderManager = new ShaderManager(
                projectShader, postProcessing, buffer.textures,
                Screen.currentResolution, geometryResolution, map,
                layerDepth, cacheSize);
        }

        void Update()
        {
            shaderManager.position = transform.position;
            shaderManager.rotation = transform.eulerAngles;
            shaderManager.fov = Camera.main.fieldOfView;
            shaderManager.debug = debug;
            LoadTexturesNearPosition(transform.position);

#if UNITY_EDITOR
            pending = new Vector3[decoder.pending.Count];
            decoding = new Vector3[decoder.decoding.Count];
            reserved = new VectorIndex[buffer.reserved.Count];
            available = new int[buffer.available.Count];

            int idx = 0;
            foreach (KeyValuePair<string, Vector3> item in decoder.pending)
                pending[idx++] = decoder.pending[item.Key];

            idx = 0;
            foreach (KeyValuePair<AsyncOperation, Tuple<Vector3, UnityWebRequest>> item in decoder.decoding)
                decoding[idx++] = item.Value.Item1;

            idx = 0;
            foreach (KeyValuePair<Vector3, int> item in buffer.reserved)
                reserved[idx++] = new VectorIndex { vector = item.Key, index = item.Value };

            idx = 0;
            foreach (int item in buffer.available)
                available[idx++] = item;

            array = buffer.textures;
#endif

            // debugOffArray[selectedId - 1] = controller.secondaryPosition;
            // shaderManager.debugPositionArray = debugOffArray;

            for (int i = layerDepth - 1; i >= 0; i--) // Furthest away first, closest last (depth sorting)
            {
                float distance = Vector3.Distance(transform.position, positionArray[i]);
                // TODO: Resolution based on distance
                int projectWidth = geometryResolution.x;
                int projectHeight = geometryResolution.y;

                // shaderManager.Project(projectWidth, projectHeight, i);
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination) =>
            shaderManager.Render(ref destination);

        private void OnDestroy()
        {
            buffer.Release();
            shaderManager.Release();
        }

        /// <summary>
        /// Loads the closest images into the buffer.
        /// Reorganizes the buffer according to the new order if the positions already exist inside it.
        /// </summary>
        void LoadTexturesNearPosition(Vector3 position)
        {
            Vector3[] newPositionArray = map.vectorOffsets.GetClosest(position, cacheSize);

            for (int i = cacheSize -1; i >= 0; i--)
            {
                Vector3 positionOffset = newPositionArray[i];

                if (positionArray.Contains(positionOffset))
                {
                    if (newPositionArray.Contains(positionOffset))
                    {
                        int j = Array.IndexOf(positionArray, positionOffset);
                        // Graphics.CopyTexture(buffer.textures, j, buffer.textures, i);
                    }
                    else buffer.Release(positionOffset);
                }
                else
                {
                    string path = newPositionArray.GetFileName(mapPath, positionOffset);
                    decoder.DecodeToBufferAsync(path, positionOffset);
                }
            }

            positionArray = newPositionArray;
            shaderManager.positionArray = positionArray;
        }
    }
}