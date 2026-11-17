using UnityEditor;
using UnityEngine;
using RoguelikePool;

public static class CreateStageAssets
{
    // Creates Assets/Data/Stages/Stage1..5.asset and Assets/Data/StageCatalog.asset with the starting values.
    // Existing assets are left untouched (their values may have been edited in the Inspector).
    public static string Run()
    {
        int[] targets = { 100, 150, 220, 320, 450 };
        const int shots = 5;
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Data/Stages")) AssetDatabase.CreateFolder("Assets/Data", "Stages");

        const string catalogPath = "Assets/Data/StageCatalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(catalogPath);
        bool newCatalog = catalog == null;
        if (newCatalog)
        {
            catalog = ScriptableObject.CreateInstance<StageCatalog>();
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }

        string report = "";
        catalog.stages.Clear();
        for (int i = 0; i < targets.Length; i++)
        {
            string path = $"Assets/Data/Stages/Stage{i + 1}.asset";
            var stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (stage == null)
            {
                stage = ScriptableObject.CreateInstance<StageData>();
                stage.targetScore = targets[i];
                stage.maxShots = shots;
                AssetDatabase.CreateAsset(stage, path);
                report += $"created {path} ({stage.targetScore}/{stage.maxShots}); ";
            }
            else report += $"kept {path} ({stage.targetScore}/{stage.maxShots}); ";
            catalog.stages.Add(stage);
        }
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return (newCatalog ? "created " : "updated ") + catalogPath + " with " + catalog.Count + " stages; " + report;
    }
}
