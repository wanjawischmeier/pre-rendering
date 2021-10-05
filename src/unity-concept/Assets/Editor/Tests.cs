#if UNITY_EDITOR
// C# tests
#if !(PROJECTION_PERCISION_LOW || PROJECTION_PERCISION_HIGH)
#error PREPROCESSOR_DEFINE: The projection percision macro is undefined or invalid. The supported definitions are <PROJECTION_PERCISION_LOW> and <PROJECTION_PERCISION_HIGH>. Please define this macro inside the project settings, under 'Player/Script Compilation/Scripting Define Symbols', then click 'Apply' and wait for the scripts to recompile.
#endif

using UnityEngine;
using UnityEditor;

class Tests
{
    [InitializeOnLoadMethod]
#pragma warning disable IDE0051 // Nicht verwendete private Member entfernen
    static void Test()
#pragma warning restore IDE0051
    {

    }
}

#elif !UNITY_STANDALONE
// HLSL tests
#if !(PERCISION_LOW || PERCISION_HIGH)
#error PREPROCESSOR_DEFINE: The percision macro is undefined or invalid. The supported definitions are <PERCISION_LOW> and <PERCISION_HIGH>. Set the macro in a C# script, based on the global projection percision macro (using SHADER.EnableKeyword).
#endif

#endif