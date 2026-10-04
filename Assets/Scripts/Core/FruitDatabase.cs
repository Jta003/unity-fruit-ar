using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class FruitDatabase : MonoBehaviour
{
    public static FruitDatabase Instance { get; private set; }
    [SerializeField] private TextAsset source;
    private readonly Dictionary<string, FruitData> byId = new Dictionary<string, FruitData>();
    public IReadOnlyCollection<FruitData> All => byId.Values;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (source == null) source = Resources.Load<TextAsset>("fruits");
        if (source == null) { Debug.LogError("Missing Assets/Resources/fruits.json"); return; }
        var catalog = JsonUtility.FromJson<FruitCatalog>(source.text);
        if (catalog?.fruits == null) { Debug.LogError("Invalid fruit catalog JSON"); return; }
        foreach (var fruit in catalog.fruits.Where(f => f != null && !string.IsNullOrWhiteSpace(f.id))) byId[fruit.id] = fruit;
    }

    public FruitData Find(string id) => id != null && byId.TryGetValue(id, out var fruit) ? fruit : null;
    public FruitData FindByLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;
        var key = label.ToLowerInvariant().Replace(" fruit", "").Trim();
        return byId.Values.FirstOrDefault(f => f.id == key || f.englishName.ToLowerInvariant() == key || f.modelKey == key);
    }
}
