using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FruitProjectBootstrap
{
    static FruitProjectBootstrap()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists("Assets/Scenes/FruitNutrition.unity")) BuildInitialProject();
            else ConfigureAndroid();
        };
    }

    [MenuItem("Fruit Nutrition AR/Build starter scene")]
    public static void BuildInitialProject()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("FruitNutritionApp", typeof(FruitNutritionApp));
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var scenePath = "Assets/Scenes/FruitNutrition.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

        ConfigureAndroid();
        AssetDatabase.SaveAssets();
        Debug.Log("Fruit Nutrition AR starter scene and Android settings created.");
    }

    private static void ConfigureAndroid()
    {
        PlayerSettings.productName = "Fruit Nutrition AR";
        PlayerSettings.companyName = "Fruit Nutrition AR";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.fruitnutrition.ar");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.forceInternetPermission = false;
        AssetDatabase.SaveAssets();
    }
}
