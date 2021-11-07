using PreRendering;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PreRenderer))]
public class PreRenderingEditor : Editor
{
    private const float SpacerMedium = 20;
    private readonly string[] shaderDebugModes = Enum.GetNames(typeof(ShaderManager.ShaderDebugMode));

    private void OnValidate()
    {
        var renderer = (PreRenderer)target;

        if (renderer.editorAreas == null || renderer.editorAreas.Length == 0)
            renderer.editorAreas = new bool[3];

        renderer.renderPath = Application.dataPath.Split(new string[] { PreRenderer.RepoName }, StringSplitOptions.None)[0];
        renderer.renderPath = Path.Combine(renderer.renderPath, PreRenderer.RepoName, "renders");

        string[] mapConfigs = Directory.GetFiles(renderer.renderPath, ".mapconfig", SearchOption.AllDirectories);

        renderer.mapPaths = new string[mapConfigs.Length];
        renderer.mapFiles = new string[mapConfigs.Length];

        for (int i = 0; i < mapConfigs.Length; i++)
        {
            renderer.mapPaths[i] = Path.GetDirectoryName(mapConfigs[i]);
            renderer.mapFiles[i] = Path.GetFileName(renderer.mapPaths[i]);
        }
    }

    public override void OnInspectorGUI()
    {
        var renderer = (PreRenderer)target;
        bool playing = Application.isPlaying;

        renderer.editorAreas[0] = EditorGUILayout.BeginFoldoutHeaderGroup(renderer.editorAreas[0], "Map");

        if (renderer.editorAreas[0])
        {
            EditorHelper.TextField(
                "Render Path", ref renderer.renderPath,
                "The folder the map should be contained in.", false);

            EditorHelper.OptionField(
                "Map Name", ref renderer.mapSelection, renderer.mapFiles,
                "The name of the folder the '.mapconfig' file is contained in. " +
                "This folder has to be inside the 'renders' parent folder.", !playing);

            GUILayout.Space(SpacerMedium);
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

            GUILayout.Space(SpacerMedium);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();




        renderer.editorAreas[2] = EditorGUILayout.BeginFoldoutHeaderGroup(renderer.editorAreas[2], "Projection & Post Processing");

        if (renderer.editorAreas[2])
        {
            EditorHelper.FloatSlider(
                "Geometry Percision", ref renderer.geometryPercision,
                "The base value the screen resolution should be divided by for projection.",
                0.1f, 1, !playing);

            EditorHelper.OptionField(
                "Shader Debug", ref renderer.shaderDebugSelection, shaderDebugModes,
                "If enabled, the post processing shader will pass the desired texture to the screen.",
                playing);

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
