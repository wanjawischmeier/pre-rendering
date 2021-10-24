using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public TextureLoader loader;
    public MovementController controller;
    public FPSCounter debugger;
    public GameObject menu;

    const string options = "Options";

    bool MenuEnabled
    {
        get { return SceneManager.sceneCount == 2; }
        set
        {
            if (value)
                SceneManager.LoadSceneAsync(options, LoadSceneMode.Additive);
            else
                SceneManager.UnloadSceneAsync(options);

            controller.enabled = !controller.enabled;
        }
    }
    
    void Start()
    {
        debugger.loader = loader;
        loader.controller = controller;
    }

    void Update()
    {
#if !UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F4)) Application.Quit();
#endif
        if (Input.GetKeyDown(KeyCode.F2)) debugger.active = !debugger.active;
        if (Input.GetKeyDown(KeyCode.F3)) loader.debug = !loader.debug;
        if (Input.GetKeyDown(KeyCode.Escape)) MenuEnabled = !MenuEnabled;
        if (Input.mouseScrollDelta.y > 0) loader.selectedId += 1;
        if (Input.mouseScrollDelta.y < 0) loader.selectedId -= 1;
        if (loader.selectedId > loader.layerDepth) loader.selectedId = 1;
        if (loader.selectedId < 1) loader.selectedId = loader.layerDepth;
    }
}