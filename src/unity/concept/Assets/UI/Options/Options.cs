using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    private InputField screenResolutionWidth, screenResolutionHeight;
    private Text cacheSizeValue;
    private Resolution nativeResolution;

    private void Start()
    {
        screenResolutionWidth = GameObject.FindGameObjectWithTag("ScreenResolutionWidth").GetComponent<InputField>();
        screenResolutionHeight = GameObject.FindGameObjectWithTag("ScreenResolutionHeight").GetComponent<InputField>();
        Dropdown dropdown = GameObject.FindGameObjectWithTag("ScreenResolutionDropdown").GetComponent<Dropdown>();
        nativeResolution = Screen.resolutions[Screen.resolutions.Length - 1];
        int width, height, resolutionType = PlayerPrefs.GetInt("Screen Resolution Type");
        if (resolutionType == 0)
        {
            width = nativeResolution.width;
            height = nativeResolution.height;
        }
        else
        {
            width = PlayerPrefs.GetInt("Screen Resolution Width");
            height = PlayerPrefs.GetInt("Screen Resolution Height");
        }
        dropdown.value = resolutionType;
        screenResolutionWidth.text = width.ToString();
        screenResolutionHeight.text = height.ToString();
        bool interactable = resolutionType != 0;
        screenResolutionWidth.interactable = interactable;
        screenResolutionHeight.interactable = interactable;

        GameObject chacheSize = GameObject.FindGameObjectWithTag("CacheSize");
        cacheSizeValue = chacheSize.GetComponentsInChildren<Text>()[1];
        Slider slider = chacheSize.GetComponentInChildren<Slider>();
        slider.value = PlayerPrefs.GetInt("Cache Size");
        cacheSizeValue.text = slider.value.ToString();

        Toggle fpsDebugger = GameObject.FindGameObjectWithTag("FPSDebugger").GetComponent<Toggle>();
        Toggle shaderDebugger = GameObject.FindGameObjectWithTag("ShaderDebugger").GetComponent<Toggle>();
        fpsDebugger.isOn = PlayerPrefs.GetInt("FPS Debugger") == 1;
        shaderDebugger.isOn = PlayerPrefs.GetInt("Shader Debugger") == 1;
    }

    public void OnScreenResolution(int value)
    {
        if (value == 0)
        {
            screenResolutionWidth.interactable = false;
            screenResolutionHeight.interactable = false;

            screenResolutionWidth.text = nativeResolution.width.ToString();
            screenResolutionHeight.text = nativeResolution.height.ToString();
        }
        else
        {
            screenResolutionWidth.interactable = true;
            screenResolutionHeight.interactable = true;

            screenResolutionWidth.text = "";
            screenResolutionHeight.text = "";
        }

        PlayerPrefs.SetInt("Screen Resolution Type", value);
    }

    public void OnScreenResolutionWidth(string text) =>
        PlayerPrefs.SetInt("Screen Resolution Width", int.Parse(text));

    public void OnScreenResolutionHeight(string text) =>
        PlayerPrefs.SetInt("Screen Resolution Height", int.Parse(text));

    public void OnCacheSize(float value)
    {
        cacheSizeValue.text = value.ToString();
        PlayerPrefs.SetInt("Cache Size", Mathf.RoundToInt(value));
    }

    public void OnFPSDebugger(bool value) =>
        PlayerPrefs.SetInt("FPS Debugger", value ? 1 : 0);

    public void OnShaderDebugger(bool value) =>
        PlayerPrefs.SetInt("Shader Debugger", value ? 1 : 0);

    public void MainMenu() =>
        SceneManager.LoadSceneAsync("Start");

    public void Apply()
    {
#if !UNITY_EDITOR
        Screen.SetResolution(
            PlayerPrefs.GetInt("Screen Resolution Width"),
            PlayerPrefs.GetInt("Screen Resolution Height"),
            false
        );
#endif
        PlayerPrefs.Save();
    }
}
