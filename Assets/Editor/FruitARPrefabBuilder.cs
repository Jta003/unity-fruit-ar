using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds fruit AR wrapper prefabs from the project-owned source models and
/// updates the existing scene registry. AppleAR is deliberately preserved.
/// </summary>
public static class FruitARPrefabBuilder
{
    private const string ScenePath = "Assets/Scenes/FruitNutrition.unity";
    private const string PrefabFolder = "Assets/Prefabs/Fruits";
    private sealed class Definition
    {
        public readonly string id, name, source, albedo, normal, occlusion;
        public readonly float targetSize, smoothness;
        public readonly Vector3 rotation;
        public readonly bool normalIsHeight;

        public Definition(string id, string name, string source, string albedo, float targetSize,
            string normal = null, string occlusion = null, float smoothness = .24f, bool normalIsHeight = false,
            Vector3 rotation = default)
        {
            this.id = id; this.name = name; this.source = source; this.albedo = albedo;
            this.targetSize = targetSize; this.normal = normal; this.occlusion = occlusion;
            this.smoothness = smoothness; this.normalIsHeight = normalIsHeight; this.rotation = rotation;
        }
    }

    // IDs and names were verified against Resources/labels.txt and Resources/fruits.json.
    // Sizes are practical first-pass real-world longest dimensions for handheld AR.
    private static readonly Definition[] Definitions =
    {
        new Definition("banana", "Banana", "Assets/Models/Fruits/banana/source/export_banana.obj",
            "Assets/Models/Fruits/banana/textures/Diffuse.png", .18f),
        new Definition("orange", "Orange", "Assets/Models/Fruits/orange/source/Orange.fbx",
            "Assets/Models/Fruits/orange/textures/Diffuse.png", .08f,
            "Assets/Models/Fruits/orange/textures/Normal.png"),
        new Definition("mango", "Mango", "Assets/Models/Fruits/mango/source/Mango.fbx",
            "Assets/Models/Fruits/mango/textures/Albedo.jpg", .12f,
            "Assets/Models/Fruits/mango/textures/Normal.jpg",
            "Assets/Models/Fruits/mango/textures/Occlusion.jpg", .22f),
        new Definition("strawberry", "Strawberry", "Assets/Models/Fruits/strawberry/source/MORANGO.fbx.fbx",
            "Assets/Models/Fruits/strawberry/textures/MORANGO.jpg", .05f,
            "Assets/Models/Fruits/strawberry/textures/BMP.jpg", null, .2f, true),
        new Definition("cherry", "Cherry", "Assets/Models/Fruits/cherry/source/cherry.obj",
            "Assets/Models/Fruits/cherry/textures/cherry_color.tga.png", .07f,
            smoothness: .32f),
        new Definition("grapes", "Grapes", "Assets/Models/Fruits/grape-king-delaware/source/grape02_Model_10.obj",
            "Assets/Models/Fruits/grape-king-delaware/textures/grape02_Model_10_u0_v0.jpg", .12f,
            null, null, .2f,
            rotation: new Vector3(-90f, 0f, 0f))
    };

    [MenuItem("Fruit Nutrition AR/Build Registered Fruit AR Prefabs")]
    public static void BuildAll()
    {
        EnsureFolder(PrefabFolder);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var prefabs = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        var apple = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/AppleAR.prefab");
        if (apple == null) throw new InvalidOperationException("The working AppleAR prefab is missing; no assets were changed.");
        prefabs.Add("apple", apple);

        foreach (var definition in Definitions)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(definition.source);
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(definition.albedo);
            if (source == null || albedo == null)
            {
                Debug.LogError($"FRUIT_AR_SKIP id={definition.id}; source={source != null}; albedo={albedo != null}; no prefab was registered.");
                continue;
            }

            try
            {
                var material = CreateMaterial(definition);
                var prefab = CreatePrefab(definition, source, material);
                if (prefab != null) prefabs[definition.id] = prefab;
            }
            catch (Exception exception)
            {
                Debug.LogException(new InvalidOperationException("Failed to prepare AR model for " + definition.id, exception));
            }
        }

        BindExistingSceneRegistry(prefabs);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("FRUIT_AR_BUILD_COMPLETE registered=" + string.Join(",", prefabs.Keys.OrderBy(id => id)) +
                  "; kiwi=deferred; chickoo=deferred");
    }

    public static void BuildAndValidate()
    {
        BuildAll();
        ValidateRegistry();
    }

    [MenuItem("Fruit Nutrition AR/Repair Cherry AR Prefab")]
    public static void RebuildCherryOnly()
    {
        var definition = Definitions.First(item => item.id == "cherry");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(definition.source);
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(definition.albedo);
        if (source == null || albedo == null)
            throw new InvalidOperationException("Cherry source model or color texture could not be loaded.");

        var material = CreateMaterial(definition);
        var prefab = CreatePrefab(definition, source, material);
        if (prefab == null) throw new InvalidOperationException("CherryAR prefab could not be saved.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateRegistry();
        Debug.Log("CHERRY_AR_REPAIRED id=cherry prefab=" + AssetDatabase.GetAssetPath(prefab) +
                  " targetLongestSideMeters=" + definition.targetSize + " normalMap=disabled occlusionMap=disabled");
    }

    [MenuItem("Fruit Nutrition AR/Validate Fruit AR Registry")]
    public static void ValidateRegistry()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        var openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            var registry = FindRegistry(scene);
            if (registry == null) throw new InvalidOperationException("FruitModelRegistry is missing from " + ScenePath);
            var serialized = new SerializedObject(registry);
            var entries = serialized.FindProperty("entries");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var id = entry.FindPropertyRelative("fruitId").stringValue;
                var prefab = entry.FindPropertyRelative("prefab").objectReferenceValue as GameObject;
                if (string.IsNullOrWhiteSpace(id) || prefab == null || !ids.Add(id))
                    throw new InvalidOperationException($"Invalid or duplicate registry entry at index {i}: id='{id}', prefab='{prefab}'.");
                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException(id + " prefab has no Renderer.");
                foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                {
                    if (transform == null) throw new InvalidOperationException(id + " prefab contains a missing GameObject reference.");
                    if (transform.GetComponents<Component>().Any(component => component == null))
                        throw new InvalidOperationException(id + " prefab contains a Missing Script component.");
                }
                foreach (var renderer in renderers)
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null) throw new InvalidOperationException(id + " has a missing material reference.");
                        var baseMap = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;
                        var mainTex = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                        if (baseMap == null && mainTex == null)
                            throw new InvalidOperationException(id + " material has no BaseColor/Albedo texture: " + material.name);
                    }
                Debug.Log($"FRUIT_AR_VALID id={id} prefab={AssetDatabase.GetAssetPath(prefab)} renderers={renderers.Length}");
            }
            var requiredIds = new[] { "apple" }.Concat(Definitions.Select(definition => definition.id));
            foreach (var requiredId in requiredIds)
                if (!ids.Contains(requiredId)) throw new InvalidOperationException("Expected registry mapping is missing: " + requiredId);
            foreach (var deferredId in new[] { "kiwi", "chickoo" })
                if (ids.Contains(deferredId)) throw new InvalidOperationException(deferredId + " must remain unregistered in this version.");
            Debug.Log("FRUIT_AR_REGISTRY_VALID count=" + ids.Count + "; kiwi/chickoo deferred");
        }
        finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
    }

    private static Material CreateMaterial(Definition definition)
    {
        var materialPath = PrefabFolder + "/" + definition.name + "AR_Material.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        var shader = GraphicsSettings.currentRenderPipeline == null
            ? Shader.Find("Standard")
            : Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("Could not resolve the active pipeline's Lit shader.");
        if (material == null)
        {
            material = new Material(shader) { name = definition.name + " AR Material" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else material.shader = shader;

        var albedo = PrepareTexture(definition.albedo, false, false);
        var normal = string.IsNullOrEmpty(definition.normal) ? null : PrepareTexture(definition.normal, true, definition.normalIsHeight);
        var occlusion = string.IsNullOrEmpty(definition.occlusion) ? null : PrepareTexture(definition.occlusion, false, false, true);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", albedo);
        if (material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normal);
            if (normal != null) material.EnableKeyword("_NORMALMAP"); else material.DisableKeyword("_NORMALMAP");
        }
        if (material.HasProperty("_OcclusionMap")) material.SetTexture("_OcclusionMap", occlusion);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", definition.smoothness);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", definition.smoothness);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D PrepareTexture(string path, bool normalMap, bool normalIsHeight, bool linear = false)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Texture importer missing: " + path);
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.anisoLevel = 4;
        if (normalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.sRGBTexture = false;
            importer.convertToNormalmap = normalIsHeight;
        }
        else importer.sRGBTexture = !linear;

        if (importer.maxTextureSize > 2048) importer.maxTextureSize = 2048;
        var android = importer.GetPlatformTextureSettings("Android");
        android.name = "Android";
        android.overridden = true;
        android.maxTextureSize = Mathf.Min(Mathf.Max(android.maxTextureSize, 1024), 2048);
        android.textureCompression = TextureImporterCompression.Compressed;
        android.compressionQuality = 50;
        importer.SetPlatformTextureSettings(android);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static GameObject CreatePrefab(Definition definition, GameObject source, Material material)
    {
        var path = PrefabFolder + "/" + definition.name + "AR.prefab";
        var root = new GameObject(definition.name + "AR");
        try
        {
            var model = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (model == null) throw new InvalidOperationException("Unity could not instantiate " + definition.source);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localRotation = Quaternion.Euler(definition.rotation) * model.transform.localRotation;

            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Source has no renderers: " + definition.source);
            foreach (var renderer in renderers)
            {
                var slots = Mathf.Max(1, renderer.sharedMaterials.Length);
                var materials = Enumerable.Repeat(material, slots).ToArray();
                renderer.sharedMaterials = materials;
            }

            var bounds = GetBounds(renderers);
            var longestSide = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (longestSide <= .000001f) throw new InvalidOperationException("Source has empty bounds: " + definition.source);
            var scaleFactor = definition.targetSize / longestSide;
            model.transform.localScale *= scaleFactor;
            bounds = GetBounds(renderers);
            model.transform.localPosition += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);

            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (saved == null) throw new InvalidOperationException("Could not save prefab at " + path);
            Debug.Log($"FRUIT_AR_PREFAB_READY id={definition.id} path={path} scaleMultiplier={scaleFactor:0.#####} bounds={bounds.size} source={definition.source} material={AssetDatabase.GetAssetPath(material)}");
            return saved;
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static Bounds GetBounds(Renderer[] renderers)
    {
        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void BindExistingSceneRegistry(Dictionary<string, GameObject> prefabs)
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        var openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("Save or discard pending edits in FruitNutrition.unity before rebuilding the registry.");
            var appObject = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(transform => transform.gameObject).FirstOrDefault(go => go.name == "FruitNutritionApp");
            if (appObject == null) throw new InvalidOperationException("FruitNutritionApp was not found in " + ScenePath);
            var app = appObject.GetComponent<FruitNutritionApp>();
            if (app == null) throw new InvalidOperationException("FruitNutritionApp component is missing from the scene object.");
            var registry = appObject.GetComponent<FruitModelRegistry>();
            if (registry == null) registry = Undo.AddComponent<FruitModelRegistry>(appObject);

            var registryData = new SerializedObject(registry);
            var entries = registryData.FindProperty("entries");
            entries.arraySize = prefabs.Count;
            var ids = prefabs.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            for (var i = 0; i < ids.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("fruitId").stringValue = ids[i];
                entry.FindPropertyRelative("prefab").objectReferenceValue = prefabs[ids[i]];
            }
            registryData.ApplyModifiedPropertiesWithoutUndo();
            var appData = new SerializedObject(app);
            appData.FindProperty("modelRegistry").objectReferenceValue = registry;
            appData.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("FRUIT_AR_REGISTRY_BOUND " + string.Join(",", ids.Select(id => id + "->" + AssetDatabase.GetAssetPath(prefabs[id]))));
        }
        finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
    }

    private static FruitModelRegistry FindRegistry(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var registry = root.GetComponentInChildren<FruitModelRegistry>(true);
            if (registry != null) return registry;
        }
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        var name = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
