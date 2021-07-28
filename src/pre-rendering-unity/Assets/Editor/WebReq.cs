using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WebReq : MonoBehaviour
{
    public string path;
    public Texture2D texture;

    void Start()
    {
        StartCoroutine(GetText());
    }
    IEnumerator GetText()
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(new System.Uri(path)))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                texture = DownloadHandlerTexture.GetContent(uwr);
            }
        }
    }
}
