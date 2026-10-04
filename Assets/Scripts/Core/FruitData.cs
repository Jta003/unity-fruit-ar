using System;

[Serializable]
public sealed class FruitData
{
    public string id;
    public string name;
    public string englishName;
    public string scientificName;
    public string modelKey;
    public float calories;
    public float sugar;
    public float carbohydrate;
    public float fiber;
    public float protein;
    public float fat;
    public float vitaminC;
    public string servingSize;
    public int healthScore;
}

[Serializable]
public sealed class FruitCatalog
{
    public FruitData[] fruits;
}

[Serializable]
public sealed class ScanRecord
{
    public string fruitId;
    public string fruitName;
    public float confidence;
    public string scannedAt;
}

[Serializable]
public sealed class ScanRecordList
{
    public ScanRecord[] items;
}
