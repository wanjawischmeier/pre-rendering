using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace PreRendering
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MovementController))]
    [HelpURL("https://github.com/wanjawischmeier/pre-rendering/")]
    public partial class PreRenderer : MonoBehaviour
    {
#if UNITY_EDITOR
        public enum Foldout
        {
            Map,
            Decoder,
            Projection,
            PostProcessing,
            Debugging
        }

        public bool[] foldouts;
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
            Map.LoadFromPath(mapPath);

            mainCamera = Camera.main;
            controller = GetComponent<MovementController>();


            projectionResolution = Map.resolution.Multiply(geometryPercision);
            screenResolution = Map.resolution.EstimateScreenResolution(mainCamera.fieldOfView);

#if !UNITY_EDITOR
            Screen.SetResolution(screenResolution.width, screenResolution.height, true);
#endif

            Decoder.Initialize(Map.GetFileName(Vector3.zero), cacheSize);
            buffer = new RawTexture.Buffer(Decoder.bufferPointer, Map.resolution.width, Map.resolution.height, cacheSize);
            decoder = new DecodingManager(buffer, decodingThreads, cacheSize);
            shaderManager = new ShaderManager(projectionResolution);

            decoder.priority = decodingPriority;

            SetShaderConstants();
        }

        private void Update()
        {
#if !UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
#endif
            decoder.Refresh();
            SetShaderValues();

            // Set debug values
            for (int i = 0; i < debuggingInts.Length; i++)
                Shader.SetGlobalInt(debuggingIntNames[i], debuggingInts[i]);

            Vector3[] positions = Map.offsets.GetClosest(transform.position, cacheSize + 1);
            // Vector3[] positions = Map.offsets.PredictClosest(lastPosition, transform.position, cacheSize, predictionBlend, predictionDistance);
            Vector3 temp;

            // Clear old pending positions if the player has moved
            if (transform.position != lastPosition) decoder.ClearPending();
            lastPosition = transform.position;

            // Load the closest image synchronously if it isn't available yet
            if (!buffer.ContainsAny(positions))
            {
                temp = positions[0];
                string path = Map.GetFileName(temp);
                decoder.DecodeToBuffer(path, temp);
                positionOffset = temp;
            }

            // Load the closest images into the buffer asynchronously
            for (int i = cacheSize; i >= 0; i--)
            {
                temp = positions[i];
                string path = Map.GetFileName(temp);

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
}