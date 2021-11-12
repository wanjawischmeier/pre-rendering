using System;
using UnityEditor;
using UnityEngine;

public static class EditorHelper
{
    private const float SpacerLabel = 120;
    private const float SpacerValue = 60;

    public static void BeginField<T>(string name, ref T value, string tooltip = "", bool enabled = true)
    {
        GUILayout.BeginHorizontal(new GUIContent("", tooltip), GUIStyle.none);
        GUILayout.Label(name, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(SpacerLabel));
        if (!enabled) EditorGUI.BeginDisabledGroup(true);
    }

    public static void EndField(bool enabled = true)
    {
        if (!enabled) EditorGUI.EndDisabledGroup();
        GUILayout.EndHorizontal();
    }

    public static void TextField(string name, ref string value, string tooltip = "", bool enabled = true)
    {
        BeginField(name, ref value, tooltip, enabled);

        value = GUILayout.TextField(value);

        EndField(enabled);
    }

    public static void IntField(string name, ref int value, string tooltip = "", bool enabled = true)
    {
        BeginField(name, ref value, tooltip, enabled);

        value = GUILayout.TextField(
            value.ToString(),
            EditorStyles.numberField)
            .ParseToInt(value);

        EndField(enabled);
    }

    public static void ColorField(string name, ref Color value, string tooltip = "", bool enabled = true)
    {
        BeginField(name, ref value, tooltip, enabled);

        value = EditorGUILayout.ColorField(value);

        EndField(enabled);
    }

    public static void OptionField(string name, ref int value, string[] options, string tooltip = "", bool enabled = true)
    {
        BeginField(name, ref value, tooltip, enabled);

        value = EditorGUILayout.Popup(value, options);

        EndField(enabled);
    }

    public static void IntSlider(string name, ref int value, string tooltip = "", int min = 0, int max = 1, bool enabled = true)
    {
        BeginField(name, ref value, tooltip, enabled);

        value = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max));

        value = GUILayout.TextField(
            value.ToString(),
            EditorStyles.numberField,
            GUILayout.MaxWidth(SpacerValue))
            .ParseToInt(value, min);

        value = Mathf.Clamp(value, min, max);

        EndField(enabled);
    }

    public static void FloatSlider(string name, ref float value, string tooltip = "", float min = 0, float max = 1, bool enabled = true)
    {
        BeginField(name, ref value, tooltip, enabled);

        value = GUILayout.HorizontalSlider(value, min, max);

        value = GUILayout.TextField(
            (Mathf.Round(value * 10000) / 10000f).ToString(),
            EditorStyles.numberField,
            GUILayout.MaxWidth(SpacerValue))
            .ParseToFloat(value, min);

        value = Mathf.Clamp(value, min, max);

        EndField(enabled);
    }

    private static int ParseToInt(this string str, int old = 0, int min = 0)
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

    private static float ParseToFloat(this string str, float old = 0, float min = 0)
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
