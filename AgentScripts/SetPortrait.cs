using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class SetPortrait
{
    // Usage: --args [width, height]. 540x960 = native pixel-art resolution (scale 1), best for previewing in a small Game view.


    public static string Run(int W, int H)
    {
        string Label = "Portrait " + W + "x" + H;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        AssetDatabase.SaveAssets();

        var asm = typeof(Editor).Assembly;
        var sizesType = asm.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance").GetValue(null);
        var groupType = sizesType.GetProperty("currentGroupType").GetValue(sizes);
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupType });
        var groupT = group.GetType();

        int index = -1;
        int count = (int)groupT.GetMethod("GetTotalCount").Invoke(group, null);
        var texts = (string[])groupT.GetMethod("GetDisplayTexts").Invoke(group, null);
        for (int i = 0; i < count; i++) if (texts[i].StartsWith(Label)) index = i;

        if (index < 0)
        {
            var sizeT = asm.GetType("UnityEditor.GameViewSize");
            var sizeTypeEnum = asm.GetType("UnityEditor.GameViewSizeType");
            var ctor = sizeT.GetConstructor(new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
            var size = ctor.Invoke(new object[] { Enum.Parse(sizeTypeEnum, "FixedResolution"), W, H, Label });
            groupT.GetMethod("AddCustomSize").Invoke(group, new[] { size });
            sizesType.GetMethod("SaveToHDD")?.Invoke(sizes, null);
            index = (int)groupT.GetMethod("GetTotalCount").Invoke(group, null) - 1;
        }

        var gameViewT = asm.GetType("UnityEditor.GameView");
        var gameView = EditorWindow.GetWindow(gameViewT, false, null, false);
        const BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var prop = gameViewT.GetProperty("selectedSizeIndex", f);
        if (prop != null && prop.CanWrite) prop.SetValue(gameView, index);
        else gameViewT.GetMethod("SizeSelectionCallback", f).Invoke(gameView, new object[] { index, null });
        gameView.Repaint();
        return "Game view size index " + index + " (" + Label + "), orientation=" + PlayerSettings.defaultInterfaceOrientation;
    }
}
