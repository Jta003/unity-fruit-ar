using UnityEditor;
using UnityEngine;

/// <summary>
/// Runs the targeted Cherry repair once in the open Unity Editor after scripts
/// compile, keeping the existing scene registry and other fruit prefabs intact.
/// </summary>
[InitializeOnLoad]
internal static class FruitARPrefabAutoBuild
{
    private const string SessionKey = "FruitNutritionAR.CherryPrefabRepair.2026-10-04.v1";

    static FruitARPrefabAutoBuild()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RunOnce;
            return;
        }

        SessionState.SetBool(SessionKey, true);
        try
        {
            FruitARPrefabBuilder.RebuildCherryOnly();
        }
        catch (System.Exception exception)
        {
            Debug.LogError("Cherry AR prefab repair did not complete. Fix the reported asset/configuration issue, then use Fruit Nutrition AR > Repair Cherry AR Prefab.");
            Debug.LogException(exception);
        }
    }
}
