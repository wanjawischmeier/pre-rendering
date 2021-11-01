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

    public static void OptionField(string name, ref int value, string[] options, string tooltip, bool enabled = true)
    {
        BeginField(name, tooltip, ref value, enabled);

        value = EditorGUILayout.Popup(value, options);

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

    static int ParseToInt(this string str, int old = 0, int min = 0)
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

    static float ParseToFloat(this string str, float old = 0, float min = 0)
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
