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
        Vector3 positionOffset;

        public Map map;
        TextureBuffer buffer;
        DecodingThread decoder;
        ShaderManager shaderManager;
        Camera mainCamera;

        const float minimumPercision = 0.1f;
        const float maximumPercision = 1;


        void Start()
        {
            Vector3 positionOffset = Vector3.zero;
            mainCamera = Camera.main;
            controller = GetComponent<MovementController>();

            mapPath = Path.Combine(renderPath, mapName);
            map = new Map(mapPath);

            projectionResolution = map.resolution.Multiply(geometryPercision);
            screenResolution = map.resolution.EstimateScreenResolution(mainCamera.fieldOfView);

#if !UNITY_EDITOR
            Screen.SetResolution(screenResolution.width, screenResolution.height, true);
#endif

            buffer = new TextureBuffer(map.resolution.width, map.resolution.height, cacheSize);
            decoder = new DecodingThread(buffer, decodingThreads);
            shaderManager = new ShaderManager(
                projectShader, buffer.textures,
                projectionResolution, map, cacheSize);
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

            // Load the closest images into the buffer
            for (int i = cacheSize -1; i >= 0; i--)
            {
                Vector3 temp = positions[i];
                string path = map.offsets.GetFileName(mapPath, temp);

                if (buffer.Contains(temp))
                    positionOffset = temp;
                else
                    decoder.DecodeToBuffer(path, temp);
            }

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