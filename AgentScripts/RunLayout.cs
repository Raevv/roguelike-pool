public static class RunLayout
{
    public static string Run(int step)
    {
        var m = System.Type.GetType("UILayoutBuilder, Assembly-CSharp-Editor").GetMethod("Step" + step);
        return (string)m.Invoke(null, null);
    }
}
