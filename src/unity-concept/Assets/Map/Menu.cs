using UnityEngine;

public class Menu : MonoBehaviour
{
    public TextureLoader loader;
    public MovementController controller;
    public FPSCounter debugger;
    public GameObject menu;

    bool menu_enabled
    {
        get { return menu.activeSelf; }
        set
        {
            menu.SetActive(value);
            controller.enabled = !value;
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
        if (Input.GetKeyDown(KeyCode.Escape)) menu_enabled = !menu_enabled;
        if (Input.mouseScrollDelta.y > 0) loader.selectedId += 1;
        if (Input.mouseScrollDelta.y < 0) loader.selectedId -= 1;
        if (loader.selectedId > loader.layerDepth) loader.selectedId = 1;
        if (loader.selectedId < 1) loader.selectedId = loader.layerDepth;
    }
}