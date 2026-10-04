using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class LocalRepository
{
    private const string FavoritesKey = "fruit_nutrition_favorites_v1";
    private const string HistoryKey = "fruit_nutrition_history_v1";
    private static HashSet<string> favorites;
    private static List<ScanRecord> history;
    private static HashSet<string> Favorites => favorites ?? (favorites = ReadSet(FavoritesKey));
    private static List<ScanRecord> History => history ?? (history = ReadHistory());

    public static bool IsFavorite(string id) => Favorites.Contains(id);
    public static void ToggleFavorite(string id) { if (!Favorites.Add(id)) Favorites.Remove(id); WriteSet(FavoritesKey, Favorites); }
    public static IReadOnlyList<string> GetFavorites() => Favorites.ToList();
    public static IReadOnlyList<ScanRecord> GetHistory() => History.ToList();
    public static void AddScan(FruitData fruit, float confidence)
    {
        History.Insert(0, new ScanRecord { fruitId = fruit.id, fruitName = fruit.englishName, confidence = confidence, scannedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
        if (History.Count > 100) History.RemoveRange(100, History.Count - 100);
        PlayerPrefs.SetString(HistoryKey, JsonUtility.ToJson(new ScanRecordList { items = History.ToArray() }));
    }
    private static HashSet<string> ReadSet(string key)
    {
        try { var wrapper = JsonUtility.FromJson<StringList>(PlayerPrefs.GetString(key, "{}")); return new HashSet<string>(wrapper?.items ?? Array.Empty<string>()); }
        catch { return new HashSet<string>(); }
    }
    private static void WriteSet(string key, HashSet<string> values) => PlayerPrefs.SetString(key, JsonUtility.ToJson(new StringList { items = values.ToArray() }));
    private static List<ScanRecord> ReadHistory()
    {
        try { return (JsonUtility.FromJson<ScanRecordList>(PlayerPrefs.GetString(HistoryKey, "{}"))?.items ?? Array.Empty<ScanRecord>()).ToList(); }
        catch { return new List<ScanRecord>(); }
    }
    [Serializable] private sealed class StringList { public string[] items; }
}
