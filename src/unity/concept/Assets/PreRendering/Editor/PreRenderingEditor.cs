using UnityEditor;
using PreRendering;
using UnityEngine;
using System.IO;

[CustomEditor(typeof(PreRenderer))]
public class PreRenderingEditor : Editor
{
    const float spacer_medium = 20;

    void OnValidate()
    {
        PreRenderer renderer = (PreRenderer)target;

        renderer.renderPath = Application.dataPath.Split(new string[] { "pre-rendering" }, System.StringSplitOptions.None)[0];
        renderer.renderPath = Path.Combine(renderer.renderPath, "pre-rendering/master/renders");
    }

    public override void OnInspectorGUI()
    {
        PreRenderer renderer = (PreRenderer)target;

        renderer.editorAreas[0] = EditorGUILayout.BeginFoldoutHeaderGroup(renderer.editorAreas[0], "Map");

        if (renderer.editorAreas[0])
        {
            EditorHelper.TextField(
                "Render Path", ref renderer.renderPath,
                "The folder the map should be contained in.", false);

            EditorHelper.TextField(
                "Map Name", ref renderer.mapName,
                "The name of the folder the '.mapconfig' file is contained in. " +
                "This folder has to be inside the 'renders' parent folder.");

            GUILayout.Space(spacer_medium);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();




        renderer.editorAreas[1] = EditorGUILayout.BeginFoldoutHeaderGroup(renderer.editorAreas[1], "Decoder");

        if (renderer.editorAreas[1])
        {
            EditorHelper.IntSlider(
                "Cache Size", ref renderer.cacheSize,
                "The size of the texture cache.",
                1, 100);

            EditorHelper.IntSlider(
                "Decoding Threads", ref renderer.decodingThreads,
                "Maximum amount of textures to be decoded at once.",
                1, 10);

            GUILayout.Space(spacer_medium);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();




        renderer.editorAreas[2] = EditorGUILayout.BeginFoldoutHeaderGroup(renderer.editorAreas[2], "Projection");

        if (renderer.editorAreas[2])
        {
            EditorHelper.ComputeShaderField(
                "Projection Shader", ref renderer.projectShader,
                "The compute shader that countains the kernels needed for projection ('Project' and 'Combine').");

            EditorHelper.FloatSlider(
                "Geometry Percision", ref renderer.geometryPercision,
                "The base value the screen resolution should be divided by for projection.",
                0.1f, 1);

            GUILayout.Space(spacer_medium);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();




        renderer.editorAreas[3] = EditorGUILayout.BeginFoldoutHeaderGroup(renderer.editorAreas[3], "Post Processing");

        if (renderer.editorAreas[3])
        {
            EditorHelper.ShaderDebugField(
                "Shader Debug", ref renderer.shaderDebug,
                "If enabled, the post processing shader will pass the desired texture to the screen.");

            EditorHelper.FloatSlider(
                "Depth of Field", ref renderer.depthOfField,
                "How much depth of field should be applied.",
                0, 1);

            EditorHelper.FloatSlider(
                "Mist Offset", ref renderer.mistOffset,
                "How close the mist should be to the player. If set to one, there will be no mist.",
                -1, 1);

            EditorHelper.FloatSlider(
                "Mist Falloff", ref renderer.mistFalloff,
                "How steep the mist density should increase.",
                0.01f, 1);

            EditorHelper.ColorField(
                "Mist Color", ref renderer.mist,
                "The color of the mist.");
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }
}
