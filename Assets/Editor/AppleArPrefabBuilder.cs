using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AppleArPrefabBuilder
{
    public const string SourcePath = "Assets/Models/Fruits/Apple/source/AppleLowPoly.fbx";
    public const string PrefabPath = "Assets/Prefabs/Fruits/AppleAR.prefab";
    public const string IndicatorMaterialPath = "Assets/Resources/ARPlacementIndicator.mat";

    [MenuItem("Fruit Nutrition AR/Create Apple AR prefab")]
    public static void Create()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        if (source == null) throw new System.InvalidOperationException("Apple FBX was not found at " + SourcePath);
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Fruits"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Fruits");

        var root = new GameObject("AppleAR");
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.transform.SetParent(root.transform, false);
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new System.InvalidOperationException("Apple FBX has no renderers");

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            var longestSide = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (longestSide <= 0f) throw new System.InvalidOperationException("Apple FBX has empty bounds");

            // Keep the imported FBX unchanged. Normalize this prefab to a 10 cm apple
            // and move its lowest rendered point to the placement surface.
            model.transform.localScale *= .10f / longestSide;
            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            model.transform.localPosition += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (prefab == null) throw new System.InvalidOperationException("Could not save " + PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"APPLE_PREFAB_READY {PrefabPath}; source={SourcePath}; renderers={renderers.Length}; originalSize={longestSide:0.###}");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [MenuItem("Fruit Nutrition AR/Create AR placement material")]
    public static void CreateIndicatorMaterial()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(IndicatorMaterialPath) != null) return;
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) throw new System.InvalidOperationException("Sprites/Default shader was not found");
        var material = new Material(shader) { color = new Color32(43, 197, 84, 255) };
        AssetDatabase.CreateAsset(material, IndicatorMaterialPath);
        AssetDatabase.SaveAssets();
        Debug.Log("APPLE_AR_INDICATOR_MATERIAL_READY " + IndicatorMaterialPath);
    }

    [MenuItem("Fruit Nutrition AR/Connect Apple AR prefab")]
    public static void BindScene()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new System.InvalidOperationException("Create AppleAR.prefab before connecting it");

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/FruitNutrition.unity", OpenSceneMode.Single);
        var appObject = GameObject.Find("FruitNutritionApp");
        if (appObject == null) throw new System.InvalidOperationException("FruitNutritionApp was not found in the scene");
        var app = appObject.GetComponent<FruitNutritionApp>();
        var registry = appObject.GetComponent<FruitModelRegistry>();
        if (registry == null) registry = appObject.AddComponent<FruitModelRegistry>();

        var registryData = new SerializedObject(registry);
        var entries = registryData.FindProperty("entries");
        entries.arraySize = 1;
        var apple = entries.GetArrayElementAtIndex(0);
        apple.FindPropertyRelative("fruitId").stringValue = "apple";
        apple.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        registryData.ApplyModifiedPropertiesWithoutUndo();

        var appData = new SerializedObject(app);
        appData.FindProperty("modelRegistry").objectReferenceValue = registry;
        appData.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("APPLE_REGISTRY_BOUND AppleAR.prefab -> fruitId apple in FruitNutrition.unity");
    }
}
