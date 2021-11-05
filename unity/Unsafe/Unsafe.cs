using System;
using UnityEngine;

public class Unsafe : MonoBehaviour
{
    void Start()
    {
        IntPtr a = new IntPtr();
        unsafe
        {
            uint* b = (uint*)a.ToPointer();
            
        }
    }
}
