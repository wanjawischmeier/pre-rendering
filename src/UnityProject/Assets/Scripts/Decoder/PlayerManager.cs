using UnityEngine;
using PreRendering;
using UnityEngine.Video;

public class PlayerManager : MonoBehaviour
{
    public int mapSize;
    [Range(1, 10)]
    public int bufferRadius;
    [Range(1, 10)]
    public int threads;
    public VideoClip map;

    Manager manager;

    void Start()
    {
        manager = new Manager(mapSize, bufferRadius, transform, threads, map);
    }

    void Update()
    {
        
    }
}
