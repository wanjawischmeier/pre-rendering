using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DownhillSimplexAbstract : MonoBehaviour
{
    public Shader shader;
    public float fac, off;
    public Vector2 x0, x1, x2, tgt;
    public Scrollbar sx1, sy1, sx2, sy2, sx3, sy3;
    public TextMeshProUGUI tx1, ty1, tx2, ty2, tx3, ty3;
    public float sensitivity = 100f;
    public int digits = 4;

    private Material material;
    private GameObject panel;
    private float rfac;

    private void Start()
    {
        material = new Material(shader);
        material.SetFloat("PI", Mathf.PI);
        material.SetFloat("PI2", Mathf.PI * 2);
        
        panel = GameObject.Find("Panel");

        rfac = Mathf.Pow(10, digits -1);
    }

    private void Update()
    {
        x1 += new Vector2(sx1.value - 0.5f, sy1.value - 0.5f) / sensitivity;
        x2 += new Vector2(sx2.value - 0.5f, sy2.value - 0.5f) / sensitivity;
        tgt += new Vector2(sx3.value - 0.5f, sy3.value - 0.5f) / sensitivity;

        float rounded = Mathf.Round(x1.x * rfac) / rfac;
        tx1.text = $"X1: {(rounded == 0 ? "0," : rounded.ToString()).PadRight(digits + 1, '0')}";
        rounded = Mathf.Round(x1.y * rfac) / rfac;
        ty1.text = $"Y1: {(rounded == 0 ? "0," : rounded.ToString()).PadRight(digits + 1, '0')}";
        rounded = Mathf.Round(x2.x * rfac) / rfac;
        tx2.text = $"X2: {(rounded == 0 ? "0," : rounded.ToString()).PadRight(digits + 1, '0')}";
        rounded = Mathf.Round(x2.y * rfac) / rfac;
        ty2.text = $"Y2: {(rounded == 0 ? "0," : rounded.ToString()).PadRight(digits + 1, '0')}";
        rounded = Mathf.Round(tgt.x * rfac) / rfac;
        tx3.text = $"X3: {(rounded == 0 ? "0," : rounded.ToString()).PadRight(digits + 1, '0')}";
        rounded = Mathf.Round(tgt.y * rfac) / rfac;
        ty3.text = $"Y3: {(rounded == 0 ? "0," : rounded.ToString()).PadRight(digits + 1, '0')}";


        material.SetFloat("FAC", fac);
        material.SetFloat("OFF", off);
        material.SetVector("X0", x0);
        material.SetVector("X1", x1);
        material.SetVector("X2", x2);
        material.SetVector("TGT", tgt);
        material.SetVector("OFFSET", -transform.position);


        if (Input.GetMouseButtonDown(0))
        {
            StopAllCoroutines();
            panel.SetActive(true);
        }
        
        if (Input.GetMouseButtonUp(0))
            StartCoroutine(enumerator());
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, material);
    }

    private IEnumerator enumerator()
    {
        yield return new WaitForSeconds(5f);
        panel.SetActive(false);
    }
}
