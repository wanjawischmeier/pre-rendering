using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace PreRendering
{
    public class PreRenderer : MonoBehaviour
    {
        public ComputeShader projectShader;
        public Shader postProcessing;
        [HideInInspector]
        public MovementController controller;

        public string mainPath;
        public string mapName;
        public Vector2Int geometryResolution;
        [Range(1, 100)]
        public int layerDepth = 4;
        [Range(1, 100)]
        public int cacheSize = 10;
        [Range(1, 10)]
        public int decodingThreads;
        public bool debug;
        public float l;

        Vector3[] offsetArray;
        Vector3[] debugOffArray;
        public int selectedId = 1;

#if UNITY_EDITOR
        public Vector3[] pending;
        public Vector3[] decoded;
        public Vector3[] off;
        public Texture2DArray array;
#endif

        public Map map;
        FrameBuffer buffer;
        Decoder decoder;
        ShaderManager shaderManager;

        void Start()
        {
            string path = Path.Combine(mainPath, mapName);
            map = new Map(path);
            buffer = new FrameBuffer(0, 0, cacheSize);
            decoder = new Decoder(buffer, decodingThreads);
            shaderManager = new ShaderManager(
                projectShader, postProcessing, buffer.textures,
                Screen.currentResolution, geometryResolution, map.config,
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
            pending = new Vector3[map.pending.Count];
            decoded = new Vector3[map.decoded.Count];
            off = map.offArray;

            int idx = 0;
            foreach (KeyValuePair<AsyncOperation, Tuple<Vector3, UnityWebRequest>> item in map.pending)
                pending[idx] = map.pending[item.Key].Item1; idx++;

            idx = 0;
            foreach (KeyValuePair<Vector3, UnityWebRequest> item in map.decoded)
                decoded[idx] = item.Key; idx++;

            array = buffer.textures;
#endif

            debugOffArray[selectedId - 1] = controller.secondaryPosition;
            debugOffBuffer.SetData(debugOffArray);

            for (int i = layerDepth - 1; i >= 0; i--) // Furthest away first, closest last (depth sorting)
            {
                float distance = Vector3.Distance(transform.position, map.offArray[i]);
                // TODO: Resolution based on distance
                int projectWidth = geometryResolution.x;
                int projectHeight = geometryResolution.y;

                shaderManager.Project(projectWidth, projectHeight, i);
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination) =>
            shaderManager.Render(ref destination);

        void LoadTexturesNearPosition(Vector3 position)
        {
            Vector3[] temp = map.config.vectorOffsets.GetClosest(position, cacheSize);
            shaderManager.positionArray = temp;

            for (int i = cacheSize; i >= 0; i--)
            {

            }
        }
    }
}