using UnityEngine;

public sealed partial class FruitNutritionApp
{
    private void ShowFruitAR(FruitData fruit, bool returnToResult)
    {
        ExitCameraMode();
        selected = fruit;
        page = Page.AR;
        ClearPage();
        SetNavigationVisible(false);
        StandardHeader(fruit.englishName + " in AR", "Move your phone to detect a surface");
        backgroundPanel.enabled = false;
        arHeaderPanel.enabled = true;
        arHeaderPanel.raycastTarget = true;
        headerLabel.color = status.color = Color.white;
        TopBack(() =>
        {
            if (returnToResult) ShowResult();
            else ShowDetail(fruit);
        });

        var card = Panel("AR fruit nutrition", safeArea, new Color(SurfaceColor.r, SurfaceColor.g, SurfaceColor.b, .95f),
            new Vector2(.06f, .09f), new Vector2(.94f, .22f));
        card.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        pageDecorations.Add(card.gameObject);
        AddCardText(card, fruit.englishName, 19, FontStyle.Bold, InkColor,
            new Vector2(.06f, .49f), new Vector2(.94f, .94f), TextAnchor.MiddleLeft);
        AddCardText(card, $"{fruit.calories:0.#} kcal  ·  Sugar {fruit.sugar:0.#} g  ·  Fiber {fruit.fiber:0.#} g",
            12, FontStyle.Normal, MutedColor, new Vector2(.06f, .08f), new Vector2(.94f, .53f), TextAnchor.MiddleLeft);
        arNutritionPanel = card.gameObject;
        arNutritionPanel.SetActive(false);

#if UNITY_EDITOR
        backgroundPanel.enabled = true;
        status.text = "AR plane placement requires an ARCore Android device";
#else
        BuildAR(fruit);
#endif
    }

    private GameObject CreatePlacementIndicator(Transform parent)
    {
        var indicator = new GameObject("Placement indicator", typeof(LineRenderer));
        indicator.transform.SetParent(parent, false);
        var material = Resources.Load<Material>("ARPlacementIndicator");
        if (material == null)
        {
            Debug.LogError("ARPlacementIndicator material is missing from Resources");
            Destroy(indicator);
            return null;
        }

        var line = indicator.GetComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = .004f;
        line.positionCount = 48;
        line.startColor = line.endColor = PrimaryColor;
        for (var i = 0; i < line.positionCount; i++)
        {
            var angle = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * .06f, .005f, Mathf.Sin(angle) * .06f));
        }
        indicator.SetActive(false);
        return indicator;
    }
}
