using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FruitModelEntry
{
    public string fruitId;
    public GameObject prefab;
}

public sealed class FruitModelRegistry : MonoBehaviour
{
    [SerializeField] private List<FruitModelEntry> entries = new List<FruitModelEntry>();

    public bool HasPrefab(string fruitId) => entries.Exists(e => e != null && e.fruitId == fruitId && e.prefab != null);

    public GameObject Create(string fruitId, Transform parent)
    {
        var entry = entries.Find(e => e != null && e.fruitId == fruitId && e.prefab != null);
        if (entry == null) { Debug.LogError("No licensed 3D fruit prefab is assigned for: " + fruitId); return null; }
        return Instantiate(entry.prefab, parent);
    }
}
