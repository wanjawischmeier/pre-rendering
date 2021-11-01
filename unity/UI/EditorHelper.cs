public static void IntField(string name, ref int value, string tooltip, bool enabled = true)
{
    BeginField(name, tooltip, ref value, enabled);

    value = GUILayout.TextField(
        value.ToString(), EditorStyles.numberField)
        .ParseToInt();

    EndField(enabled);
}