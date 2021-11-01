using System.IO;
using System.Linq;
using UnityEngine;

namespace PreRendering
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MovementController))]
    [HelpURL("https://github.com/wanjawischmeier/pre-rendering/")]
    public class PreRenderer : MonoBehaviour
    {
#if UNITY_EDITOR
        public bool[] editorAreas;
#endif

        // Map
        public string renderPath;
        public string[] mapPaths;
        public string[] mapFiles;
        public int mapSelection;
        string mapName;
        string mapPath;

        // Decoder
        public int cacheSize = 10;
        public float predictionBlend = 0.75f;
        public float predictionDistance = 2;
        public int decodingThreads = 4;

        // Projection & Post Processing
        public float geometryPercision = 0.75f;
        public ShaderManager.ShaderDebugMode shaderDebug = ShaderManager.ShaderDebugMode.Disabled;
        public int shaderDebugSelection;
        public float depthOfField = 0;
        public float mistOffset = 1;
        public float mistFalloff = 0.1f;
        public Color mist = Color.white;


        public MovementController controller;
        public Resolution projectionResolution;
        public Resolution screenResolution;
        Vector3 positionOffset = default;
        Vector3 lastPosition = default;

        public Map map;
        TextureBuffer buffer;
        DecodingThread decoder;
        ShaderManager shaderManager;
        Camera mainCamera;

        public int pending, decoding;

        const float minimumPercision = 0.1f;
        const float maximumPercision = 1;


        private void Start()
        {
#if UNITY_EDITOR
            string rootPath = Application.dataPath.Split(new string[] { "pre-rendering" }, System.StringSplitOptions.None)[0];
            renderPath = Path.Combine(rootPath, "pre-rendering/master/renders");

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

            buffer = new TextureBuffer(map.resolution.width, map.resolution.height, cacheSize);
            decoder = new DecodingThread(buffer, decodingThreads, cacheSize);
            shaderManager = new ShaderManager(buffer.textures, projectionResolution, map, cacheSize);
        }

        private void Update()
        {
#if !UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
#endif

            // Set shader values
            shaderManager.Position = transform.position;
            shaderManager.Rotation = transform.eulerAngles;
            shaderManager.Fov = mainCamera.fieldOfView;
            shaderManager.DOFIntensity = depthOfField;
            shaderManager.ShaderDebug = (ShaderManager.ShaderDebugMode)shaderDebugSelection;
            shaderManager.Mist = mist;
            shaderManager.MistFalloff = mistFalloff;
            shaderManager.MistOffset = mistOffset;

            Vector3[] positions = map.offsets.GetClosest(transform.position, cacheSize);
            // Vector3[] positions = map.offsets.PredictClosest(lastPosition, transform.position, cacheSize, predictionBlend, predictionDistance);
            Vector3 temp;

            // Clear old pending positions if the player has moved
            if (transform.position != lastPosition) decoder.ClearPending();
            lastPosition = transform.position;
            
            // Load the closest image synchronously if it isn't available yet
            if (!buffer.ContainsAny(positions))
            {
                temp = positions[0];
                string path = map.offsets.GetFileName(mapPath, temp);
                decoder.DecodeToBuffer(path, temp);
                positionOffset = temp;
            }
            
            // Load the closest images into the buffer asynchronously
            for (int i = cacheSize - 1; i >= 0; i--)
            {
                temp = positions[i];
                string path = map.offsets.GetFileName(mapPath, temp);

                if (buffer.Contains(temp))
                    positionOffset = temp;
                else
                    decoder.DecodeToBufferAsync(path, temp);
            }

            // Project
            shaderManager.PositionOffset = positionOffset;
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
            buffer.Release();
            decoder.Release();
            shaderManager.Release();
        }
    }
}