using System;
using PreRendering;
using UnityEditor;
using UnityEngine;

public static class EditorHelper
{
    const float spacer_label = 120;
    const float spacer_value = 60;

    public static void BeginField<T>(string name, string tooltip, ref T value, bool enabled = true)
    {
        GUILayout.BeginHorizontal(new GUIContent("", tooltip), GUIStyle.none);
        GUILayout.Label(name, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(spacer_label));
        if (!enabled) EditorGUI.BeginDisabledGroup(true);
    }

    public static void EndField(bool enabled = true)
    {
        if (!enabled) EditorGUI.EndDisabledGroup();
        GUILayout.EndHorizontal();
    }

    public static void IntField(string name, ref int value, string tooltip, bool enabled = true)
    {
        BeginField(name, tooltip, ref value, enabled);

        value = GUILayout.TextField(
            value.ToString(), EditorStyles.numberField)
            .ParseToInt();

        EndField(enabled);
    }

    public static void TextField(string name, ref string value, string tooltip, bool enabled = true)
    {
        BeginField(name, tooltip, ref value, enabled);

        value = GUILayout.TextField(value);

        EndField(enabled);
    }

    public static void ColorField(string name, ref Color value, string tooltip, bool enabled = true)
    {
        BeginField(name, tooltip, ref value, enabled);

        value = EditorGUILayout.ColorField(value);

        EndField(enabled);
    }

    public static void ShaderField(string name, ref Shader value, string tooltip, bool enabled = true)
    {
        BeginField(name, tooltip, ref value, enabled);

        value = (Shader)EditorGUILayout.ObjectField(value, typeof(Shader), false);

        EndField(enabled);
    }

    public static void ComputeShaderField(string name, ref ComputeShader value, string tooltip, bool enabled = true)
    {
        BeginField(name, tooltip, ref value, enabled);

        value = (ComputeShader)EditorGUILayout.ObjectField(value, typeof(ComputeShader), false);

        EndField(enabled);
    }

    public static void ShaderDebugField(string name, ref ShaderManager.ShaderDebugMode value, string tooltip, bool enabled = true)
    {
        BeginField(name, tooltip, ref value, enabled);

        value = (ShaderManager.ShaderDebugMode)EditorGUILayout.EnumFlagsField(value);

        EndField(enabled);
    }

    public static void IntSlider(string name, ref int value, string tooltip, int min, int max, bool enabled = true)
    {
        BeginField(name, tooltip, ref value, enabled);

        value = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max));

        value = GUILayout.TextField(
            value.ToString(),
            EditorStyles.numberField,
            GUILayout.MaxWidth(spacer_value))
            .ParseToInt(value, min);

        value = Mathf.Clamp(value, min, max);

        EndField(enabled);
    }

    public static void FloatSlider(string name, ref float value, string tooltip, float min, float max, bool enabled = true)
    {
        BeginField(name, tooltip, ref value, enabled);

        value = GUILayout.HorizontalSlider(value, min, max);

        value = GUILayout.TextField(
            (Mathf.Round(value * 10000) / 10000f).ToString(),
            EditorStyles.numberField,
            GUILayout.MaxWidth(spacer_value))
            .ParseToFloat(value, min);

        value = Mathf.Clamp(value, min, max);

        EndField(enabled);
    }

    public static int ParseToInt(this string str, int old = 0, int min = 0)
    {
        try
        {
            return int.Parse(str == "" ? min.ToString() : str);
        }
        catch (FormatException)
        {
            return old;
        }
    }

    public static float ParseToFloat(this string str, float old = 0, float min = 0)
    {
        try
        {
            return float.Parse(str == "" ? min.ToString() : str);
        }
        catch (FormatException)
        {
            return old;
        }
    }
}
