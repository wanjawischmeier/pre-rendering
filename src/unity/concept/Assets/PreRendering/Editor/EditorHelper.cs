using System;
using UnityEditor;
using UnityEngine;

public static class EditorHelper
{
    public static void BeginField<T>(string name, ref T value, string tooltip = "", bool enabled = true)
    {
        GUILayout.BeginHorizontal(new GUIContent("", tooltip), GUIStyle.none);
        GUILayout.Label(name, GUILayout.ExpandWidth(true), GUILayout.Width(EditorGUIUtility.labelWidth));
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

    public static void IntSlider(string name, ref int value, string tooltip = "", int min = 0, int max = 1, bool enabled = true, params SpecialCondition[] specialConditions)
    {
        BeginField(name, ref value, tooltip, enabled);

        int tmp = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max));
        value = value == int.MinValue ? value : tmp;

        value = GUILayout.TextField(
            value == int.MinValue ? "" : value.ToString(),
            EditorStyles.numberField,
            GUILayout.Width(EditorGUIUtility.fieldWidth))
            .ParseToInt(min, max);

        EndField(enabled);
    }

    public static void FloatSlider(string name, ref float value, string tooltip = "", float min = 0, float max = 1, bool enabled = true, params SpecialCondition[] specialConditions)
    {
        BeginField(name, ref value, tooltip, enabled);

        float tmp = GUILayout.HorizontalSlider(value, min, max);
        value = value == float.MinValue ? value : tmp;

        value = GUILayout.TextField(
            (Mathf.Round(value * 100) / 100f).ToString(),
            EditorStyles.numberField,
            GUILayout.Width(EditorGUIUtility.fieldWidth))
            .ParseToFloat(min, max);

        EndField(enabled);
    }

    private static int ParseToInt(this string str, int min = 0, int max = 1)
    {
        if (str == "") return int.MinValue;

        try
        {
            return Mathf.Clamp(int.Parse(str), min, max);
        }
        catch (FormatException)
        {
            return int.MinValue;
        }
    }

    private static float ParseToFloat(this string str, float min = 0, float max = 1)
    {
        if (str == "") return float.MinValue;

        try
        {
            return Mathf.Clamp(float.Parse(str), min, max);
        }
        catch (FormatException)
        {
            return float.MinValue;
        }
    }

    public struct SpecialCondition
    {
        public string value;
        public string text;

        public string Check(string input)
        {
            return input == value ? text : input;
        }
    }
}