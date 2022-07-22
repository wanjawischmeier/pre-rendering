using UnityEngine;
using UnityEngine.UI;

public class StickySlider : MonoBehaviour
{
    private Scrollbar scrollbar;

    private void Start()
    {
        scrollbar = GetComponent<Scrollbar>();

    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            scrollbar.value = 0.5f;
        }
    }
}
