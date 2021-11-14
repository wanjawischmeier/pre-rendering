[AttributeUsage(AttributeTargets.Field)]
public class ShowInEditorAttribute : Attribute
{
    public readonly string name, tooltip;
    public readonly Type qualifier;
    public static class Qualifier
    {
        public static class Disabled
        {
            public static bool IsEnabled(bool playing) { return false; }
        }
        public static class Enabled
        {
            public static bool IsEnabled(bool playing) { return true; }
        }
        public static class IfPlaying
        {
            public static bool IsEnabled(bool playing) { return playing; }
        }
        public static class IfNotPlaying
        {
            public static bool IsEnabled(bool playing) { return !playing; }
        }
    }
    public ShowInEditorAttribute(string name, string tooltip, Type qualifier)
    {
        this.name = name;
        this.tooltip = tooltip;
        this.qualifier = qualifier;
    }
    public static ShowInEditorAttribute[] GetFields(Type type)
    {
        return type
            .GetFields()
            .Select(member => (ShowInEditorAttribute[])member
            .GetCustomAttributes(typeof(ShowInEditorAttribute), true))
            .SelectMany(x => x)
            .ToArray();
    }
}