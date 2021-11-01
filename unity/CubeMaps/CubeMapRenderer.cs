using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CubeMapRenderer : MonoBehaviour
{
	public Shader cubeToEqui;
	public Shader shading;
	public Shader getD;
	Material cubeToEquiMat;
	Material shadingMat;
	Material getDMat;
	public Size RenderResolution = Size.Default;
	public RenderTexture cubemap;
	public RenderTexture panorama;
	public RenderTexture depth;
	private Camera cam;
	public GameObject child;

	public enum Size
	{
		High = 2048,
		Default = 1024,
		Low = 512,
		Minimum = 256
	}

	private void OnEnable()
	{
		cubeToEquiMat = new Material(cubeToEqui);
		shadingMat = new Material(shading);
		getDMat = new Material(getD);

		child = new GameObject();
		child.hideFlags = HideFlags.HideInHierarchy;
		child.transform.SetParent(transform);
		child.transform.localPosition = Vector3.zero;
		child.transform.localEulerAngles = Vector3.zero;
		child.SetActive(false);

		cubemap = new RenderTexture((int)RenderResolution, (int)RenderResolution, 0, RenderTextureFormat.ARGB32);
		panorama = new RenderTexture((int)RenderResolution, (int)RenderResolution, 0, RenderTextureFormat.ARGB32);
		depth = new RenderTexture((int)RenderResolution, (int)RenderResolution, 0, RenderTextureFormat.ARGB32);
		cubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
		panorama.wrapMode = TextureWrapMode.Repeat;
		
		cam = child.AddComponent<Camera>();
		cam.CopyFrom(GetComponent<Camera>());
		cam.targetTexture = cubemap;
		cam.depthTextureMode = DepthTextureMode.Depth;
	}

	private void OnDisable()
	{
		if (cubemap != null) cubemap.Release();
		if (panorama != null) panorama.Release();
	}

	private void OnRenderImage(RenderTexture src, RenderTexture des)
	{
		cam.RenderToCubemap(cubemap);
		Shader.SetGlobalFloat("FORWARD", cam.transform.eulerAngles.y * Mathf.Deg2Rad);
		Shader.SetGlobalFloat("PI", Mathf.PI);
		Shader.SetGlobalFloat("PI2", Mathf.PI * 2);
		shadingMat.SetFloat("FOV", cam.fieldOfView * Mathf.Deg2Rad);
		shadingMat.SetVector("Rotation", transform.eulerAngles * Mathf.Deg2Rad);

		Graphics.Blit(src, depth, getDMat);
		Graphics.Blit(cubemap, panorama, cubeToEquiMat);
		Graphics.Blit(panorama, des, shadingMat);
	}
}