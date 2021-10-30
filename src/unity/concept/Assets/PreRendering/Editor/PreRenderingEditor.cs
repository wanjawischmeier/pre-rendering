using UnityEditor;
using PreRendering;
using UnityEngine;
using System.IO;

[CustomEditor(typeof(PreRenderer))]
public class PreRenderingEditor : Editor
{
    const float spacer_medium = 20;

    private void OnEnable() => OnValidate(); // Manually refresh render path

    void OnValidate()
    {
        PreRenderer renderer = (PreRenderer)target;

        if (renderer.editorAreas == null) renderer.editorAreas = new bool[3];

        renderer.renderPath = Application.dataPath.Split(new string[] { "pre-rendering" }, System.StringSplitOptions.None)[0];
        renderer.renderPath = Path.Combine(renderer.renderPath, "pre-rendering/master/renders");
    }

    public override void OnInspectorGUI()
    {
        PreRenderer renderer = (PreRenderer)target;
        bool playing = Application.isPlaying;

        renderer.editorAreas[0] = EditorGUILayout.BeginFoldoutHeaderGroup(renderer.editorAreas[0], "Map");

        if (renderer.editorAreas[0])
        {
            EditorHelper.TextField(
                "Render Path", ref renderer.renderPath,
                "The folder the map should be contained in.", false);

            EditorHelper.TextField(
                "Map Name", ref renderer.mapName,
                "The name of the folder the '.mapconfig' file is contained in. " +
                "This folder has to be inside the 'renders' parent folder.", !playing);

            GUILayout.Space(spacer_medium);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();




        renderer.editorAreas[1] = EditorGUILayout.BeginFoldoutHeaderGroup(renderer.editorAreas[1], "Decoder");

        if (renderer.editorAreas[1])
        {
            EditorHelper.IntSlider(
                "Cache Size", ref renderer.cacheSize,
                "The size of the texture cache.",
                1, 100, !playing);

            EditorHelper.FloatSlider(
                "Prediction Blend", ref renderer.predictionBlend,
                "How much the predicted future position should affect distance calculations.",
                0, 1);

            EditorHelper.FloatSlider(
                "Prediction Distance", ref renderer.predictionDistance,
                "How far away the predicted position should be from the current position.",
                1, 4, renderer.predictionBlend != 0);

            EditorHelper.IntSlider(
                "Decoding Threads", ref renderer.decodingThreads,
                "Maximum amount of textures to be decoded at once.",
                1, 10, !playing);

            if (playing)
            {
                EditorHelper.IntSlider(
                    "Pending", ref renderer.pending,
                    "", 0, renderer.cacheSize, false);

                EditorHelper.IntSlider(
                    "Decoding", ref renderer.decoding,
                    "", 0, renderer.decodingThreads, false);
            }

            GUILayout.Space(spacer_medium);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();


        

        renderer.editorAreas[2] = EditorGUILayout.BeginFoldoutHeaderGroup(renderer.editorAreas[2], "Projection & Post Processing");

        if (renderer.editorAreas[2])
        {
            EditorHelper.FloatSlider(
                "Geometry Percision", ref renderer.geometryPercision,
                "The base value the screen resolution should be divided by for projection.",
                0.1f, 1, !playing);

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

            EditorGUI.BeginDisabledGroup(renderer.mistOffset == 1);

            EditorHelper.FloatSlider(
                "Mist Falloff", ref renderer.mistFalloff,
                "How steep the mist density should increase.",
                0.01f, 1);

            EditorHelper.ColorField(
                "Mist Color", ref renderer.mist,
                "The color of the mist.");

            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }
}
