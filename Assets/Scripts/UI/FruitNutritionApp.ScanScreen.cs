using System;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class FruitNutritionApp
{
    private static Sprite scanCircleSprite;
    private static readonly Color GlassColor = new Color32(20, 29, 27, 232);
    private static readonly Color GlassControlColor = new Color32(25, 34, 31, 226);
    private static readonly Color GlassTileColor = new Color32(255, 255, 255, 24);

    private void CreateCameraSurface(Texture texture = null)
    {
        var viewport = new GameObject("Camera surface", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(safeArea, false);
        previewViewport = viewport.GetComponent<RectTransform>();
        previewViewport.anchorMin = Vector2.zero;
        previewViewport.anchorMax = Vector2.one;
        previewViewport.offsetMin = previewViewport.offsetMax = Vector2.zero;
        previewViewport.SetAsFirstSibling();
        viewport.GetComponent<Image>().color = new Color32(22, 28, 25, 255);

        var image = new GameObject("Camera image", typeof(RectTransform), typeof(RawImage));
        image.transform.SetParent(viewport.transform, false);
        preview = image.GetComponent<RawImage>();
        preview.texture = texture;
        preview.raycastTarget = false;
        if (texture != null)
        {
            var layout = image.AddComponent<CameraImageLayout>();
            layout.Rotation = FruitScanService.Instance.LastCaptureRotationAngle;
            layout.Mirrored = FruitScanService.Instance.LastCaptureMirrored;
        }

        Panel("Camera contrast", viewport.transform, new Color(0, 0, 0, .12f), Vector2.zero, Vector2.one);
        backgroundPanel.enabled = false;
        headerLabel.gameObject.SetActive(false);
        status.gameObject.SetActive(false);
        body.anchorMin = Vector2.zero;
        body.anchorMax = Vector2.one;
    }

    private RectTransform AddGlassPanel(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var panel = Panel(name, parent, GlassColor, min, max);
        var outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1, 1, 1, .16f);
        outline.effectDistance = new Vector2(1, -1);
        var shadow = panel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, .18f);
        shadow.effectDistance = new Vector2(0, -3);
        return panel;
    }

    private void AddScanHeader(string title, string subtitle)
    {
        var header = AddGlassPanel("Scan glass header", body, new Vector2(.025f, .875f), new Vector2(.975f, .99f));
        AddCircleControl(header, "Close scan", "×", null, ShowHome,
            new Vector2(.035f, .18f), new Vector2(.19f, .82f), 38);
        AddCardText(header, title, 22, FontStyle.Bold, Color.white,
            new Vector2(.20f, .48f), new Vector2(.96f, .91f), TextAnchor.MiddleCenter);
        AddCardText(header, subtitle, 12, FontStyle.Normal, new Color32(223, 231, 226, 255),
            new Vector2(.20f, .12f), new Vector2(.96f, .51f), TextAnchor.MiddleCenter);
    }

    private void AddScanInstruction(string message)
    {
        var pill = Panel("Scan instruction", body, new Color32(20, 45, 35, 232),
            new Vector2(.115f, .79f), new Vector2(.885f, .855f));
        var outline = pill.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(53, 185, 95, 170);
        outline.effectDistance = new Vector2(1, -1);
        var accent = MakeRect("Instruction accent", pill);
        accent.anchorMin = accent.anchorMax = new Vector2(.075f, .5f);
        accent.sizeDelta = new Vector2(9, 9);
        accent.gameObject.AddComponent<Image>().color = new Color32(93, 231, 151, 255);
        AddCardText(pill, message, 14, FontStyle.Normal, Color.white,
            new Vector2(.14f, .06f), new Vector2(.96f, .94f), TextAnchor.MiddleCenter);
    }

    private void AddCameraStatus()
    {
        var statusPanel = Panel("Camera status glass", body, new Color32(20, 29, 27, 190),
            new Vector2(.14f, .72f), new Vector2(.86f, .775f));
        status.transform.SetParent(body, false);
        status.rectTransform.anchorMin = new Vector2(.16f, .72f);
        status.rectTransform.anchorMax = new Vector2(.84f, .775f);
        status.rectTransform.offsetMin = status.rectTransform.offsetMax = Vector2.zero;
        status.gameObject.SetActive(true);
        status.fontSize = 12;
        status.color = new Color32(242, 246, 243, 255);
        status.alignment = TextAnchor.MiddleCenter;
        status.horizontalOverflow = HorizontalWrapMode.Wrap;
        status.verticalOverflow = VerticalWrapMode.Truncate;
        status.text = "กำลังเตรียมกล้อง...";
        statusPanel.SetAsFirstSibling();
    }

    private void AddScanGuide(bool resultState)
    {
        var frame = MakeRect("Fruit focus frame", body);
        frame.anchorMin = resultState ? new Vector2(.17f, .54f) : new Vector2(.17f, .385f);
        frame.anchorMax = resultState ? new Vector2(.83f, .78f) : new Vector2(.83f, .695f);
        frame.offsetMin = frame.offsetMax = Vector2.zero;
        var guide = frame.gameObject.AddComponent<ScanFocusGraphic>();
        guide.color = Color.white;
        guide.raycastTarget = false;
        guide.SetAllDirty();
    }

    private void AddDistanceHint(float bottom, float top)
    {
        var hint = Panel("Camera distance hint", body, new Color32(20, 29, 27, 204),
            new Vector2(.22f, bottom), new Vector2(.78f, top));
        AddCardText(hint, "ถือกล้องให้ห่างประมาณ 15–30 ซม.", 12, FontStyle.Normal,
            new Color32(248, 249, 247, 255), new Vector2(.04f, .06f), new Vector2(.96f, .94f), TextAnchor.MiddleCenter);
    }

    private Button AddCircleControl(Transform parent, string objectName, string label, Sprite icon, Action action,
        Vector2 anchorMin, Vector2 anchorMax, float diameter)
    {
        var root = MakeRect(objectName, parent);
        root.anchorMin = anchorMin;
        root.anchorMax = anchorMax;
        root.offsetMin = root.offsetMax = Vector2.zero;

        var hitArea = root.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        hitArea.raycastTarget = true;
        var button = root.gameObject.AddComponent<Button>();
        button.onClick.AddListener(() => action());
        button.transition = Selectable.Transition.ColorTint;
        var circle = MakeRect("Glass circle", root);
        circle.anchorMin = circle.anchorMax = new Vector2(.5f, .68f);
        circle.sizeDelta = new Vector2(diameter, diameter);
        var circleImage = circle.gameObject.AddComponent<Image>();
        circleImage.sprite = GetScanCircleSprite();
        circleImage.color = GlassControlColor;
        circleImage.raycastTarget = false;
        button.targetGraphic = circleImage;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(219, 255, 230, 255);
        colors.selectedColor = colors.normalColor;
        colors.pressedColor = new Color32(180, 240, 197, 255);
        colors.fadeDuration = .08f;
        button.colors = colors;

        if (icon != null)
        {
            var iconRect = MakeRect("Control icon", circle);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(.5f, .5f);
            iconRect.sizeDelta = new Vector2(diameter * .45f, diameter * .45f);
            var image = iconRect.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }
        else if (!string.IsNullOrEmpty(label) && label == "×")
        {
            AddCardText(circle, label, 28, FontStyle.Normal, Color.white,
                new Vector2(.05f, .02f), new Vector2(.95f, .98f), TextAnchor.MiddleCenter);
        }

        if (label != "×")
            AddCardText(root, label, 10, FontStyle.Normal, new Color32(248, 249, 247, 255),
                new Vector2(.02f, .02f), new Vector2(.98f, .28f), TextAnchor.MiddleCenter);
        return button;
    }

    private Button AddCaptureControl()
    {
        var root = MakeRect("Capture control", body);
        root.anchorMin = new Vector2(.365f, .025f);
        root.anchorMax = new Vector2(.635f, .205f);
        root.offsetMin = root.offsetMax = Vector2.zero;
        var hitArea = root.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        hitArea.raycastTarget = true;
        var button = root.gameObject.AddComponent<Button>();
        button.onClick.AddListener(Capture);
        button.transition = Selectable.Transition.ColorTint;

        var outer = MakeRect("Capture white ring", root);
        outer.anchorMin = outer.anchorMax = new Vector2(.5f, .68f);
        outer.sizeDelta = new Vector2(82, 82);
        var ring = outer.gameObject.AddComponent<Image>();
        ring.sprite = GetScanCircleSprite();
        ring.color = Color.white;
        ring.raycastTarget = false;
        var shadow = outer.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, .25f);
        shadow.effectDistance = new Vector2(0, -2);

        var center = MakeRect("Capture green center", outer);
        center.anchorMin = center.anchorMax = new Vector2(.5f, .5f);
        center.sizeDelta = new Vector2(66, 66);
        var centerImage = center.gameObject.AddComponent<Image>();
        centerImage.sprite = GetScanCircleSprite();
        centerImage.color = PrimaryColor;
        centerImage.raycastTarget = false;
        button.targetGraphic = centerImage;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(214, 255, 228, 255);
        colors.selectedColor = colors.normalColor;
        colors.pressedColor = new Color32(172, 236, 190, 255);
        colors.fadeDuration = .08f;
        button.colors = colors;
        AddCardText(root, "ถ่ายภาพ", 11, FontStyle.Normal, Color.white,
            new Vector2(.02f, .02f), new Vector2(.98f, .25f), TextAnchor.MiddleCenter);
        return button;
    }

    private Button AddResultAction(Transform parent, string label, Action action, Vector2 min, Vector2 max, bool primary, bool enabled)
    {
        Color buttonColor = primary ? PrimaryColor : (Color)new Color32(255, 255, 255, 34);
        var rect = Panel(label, parent, buttonColor, min, max);
        var image = rect.GetComponent<Image>();
        image.raycastTarget = true;
        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = primary ? new Color(1, 1, 1, .16f) : new Color(1, 1, 1, .34f);
        outline.effectDistance = new Vector2(1, -1);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.interactable = enabled;
        button.onClick.AddListener(() => action());
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = primary ? new Color32(218, 255, 228, 255) : new Color32(230, 238, 233, 255);
        colors.selectedColor = colors.normalColor;
        colors.pressedColor = primary ? new Color32(182, 244, 199, 255) : new Color32(205, 216, 210, 255);
        colors.disabledColor = new Color32(142, 155, 149, 255);
        colors.fadeDuration = .08f;
        button.colors = colors;
        Color textColor = enabled ? Color.white : (Color)new Color32(224, 230, 226, 255);
        AddCardText(rect, label, 13, FontStyle.Bold, textColor,
            new Vector2(.035f, .04f), new Vector2(.965f, .96f), TextAnchor.MiddleCenter);
        return button;
    }

    private static Sprite GetScanCircleSprite()
    {
        if (scanCircleSprite != null) return scanCircleSprite;
        const int size = 128;
        const float radius = 62f;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(size * .5f, size * .5f));
            pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius + .5f - distance));
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        scanCircleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100);
        return scanCircleSprite;
    }

    private void AddBottomUtilities(bool resultState)
    {
        var controlMin = resultState ? new Vector2(.08f, .01f) : new Vector2(.085f, .035f);
        var controlMax = resultState ? new Vector2(.28f, .16f) : new Vector2(.285f, .205f);
        AddCircleControl(body, "History shortcut", "ประวัติ", LoadArt("nav_history"),
            () => ShowList(Page.History), controlMin, controlMax, resultState ? 42 : 52);
        if (resultState)
        {
            var again = AddResultAction(body, "สแกนอีกครั้ง", OpenScan,
                new Vector2(.36f, .035f), new Vector2(.64f, .12f), false, true);
            var againImage = again.GetComponent<Image>();
            againImage.color = new Color32(25, 34, 31, 220);
        }
        else
        {
            captureButton = AddCaptureControl();
            captureButton.interactable = false;
        }
        AddCircleControl(body, "Fruit library shortcut", "เลือกผลไม้", LoadArt("nav_search"),
            () => ShowList(Page.Fruits), resultState ? new Vector2(.72f, .01f) : new Vector2(.715f, .035f),
            resultState ? new Vector2(.92f, .16f) : new Vector2(.915f, .205f), resultState ? 42 : 52);
    }

    private void OpenScanPage(bool showNutritionInAR)
    {
        ExitCameraMode();
        openArAfterCapture = showNutritionInAR;
        page = Page.Scan;
        ClearPage();
        SetNavigationVisible(false);
        CreateCameraSurface();
        AddScanHeader("Scan Fruit", "สแกนผลไม้เพื่อดูข้อมูลโภชนาการ");
        AddScanInstruction("วางผลไม้ให้อยู่ในกรอบ และถ่ายให้ชัดเจน");
        AddCameraStatus();
        AddScanGuide(false);
        AddDistanceHint(.325f, .38f);
        AddBottomUtilities(false);
        StartCoroutine(StartCamera());
    }

    private void ShowResult()
    {
        ExitCameraMode();
        page = Page.Result;
        ClearPage();
        SetNavigationVisible(false);
        // Keep the captured frame behind the result card as the scan backdrop.
        CreateCameraSurface(FruitScanService.Instance.LastCapturedFrame);
        AddScanHeader("ผลการสแกน", "พบผลไม้แล้ว · " + confidence.ToString("P0"));
        AddScanInstruction("วางผลไม้ให้อยู่ในกรอบ และถ่ายให้ชัดเจน");
        AddScanGuide(true);
        AddDistanceHint(.48f, .525f);

        var card = AddGlassPanel("Fruit result glass card", body, new Vector2(.035f, .18f), new Vector2(.965f, .45f));
        var artBack = Panel("Fruit art backing", card, SoftGreenColor,
            new Vector2(.15f, .82f), new Vector2(.15f, .82f));
        artBack.sizeDelta = new Vector2(68, 68);
        var hasFruitArt = AddFruitArt(card, selected, new Vector2(.15f, .82f), 58);
        if (!hasFruitArt) artBack.gameObject.SetActive(false);
        float identityLeft = hasFruitArt ? .305f : .085f;

        var detected = Panel("Detected chip", card, new Color32(43, 133, 79, 255),
            new Vector2(identityLeft, .84f), new Vector2(hasFruitArt ? .57f : .44f, .965f));
        AddCardText(detected, "พบผลไม้", 10, FontStyle.Bold, new Color32(208, 255, 224, 255),
            new Vector2(.05f, .04f), new Vector2(.95f, .96f), TextAnchor.MiddleCenter);
        AddCardText(card, selected.englishName, 20, FontStyle.Bold, Color.white,
            new Vector2(identityLeft, .65f), new Vector2(hasFruitArt ? .75f : .76f, .83f), TextAnchor.MiddleLeft);
        AddCardText(card, selected.scientificName, 10, FontStyle.Normal, new Color32(207, 218, 212, 255),
            new Vector2(identityLeft, .53f), new Vector2(hasFruitArt ? .76f : .76f, .65f), TextAnchor.MiddleLeft);

        var confidenceRing = MakeRect("Confidence ring", card);
        confidenceRing.anchorMin = confidenceRing.anchorMax = new Vector2(.86f, .81f);
        confidenceRing.sizeDelta = new Vector2(58, 58);
        var ringImage = confidenceRing.gameObject.AddComponent<Image>();
        ringImage.sprite = CreateHealthRingSprite(Mathf.Clamp01(confidence));
        ringImage.raycastTarget = false;
        AddCardText(confidenceRing, confidence.ToString("P0"), 12, FontStyle.Bold, Color.white,
            new Vector2(.08f, .08f), new Vector2(.92f, .92f), TextAnchor.MiddleCenter);
        AddCardText(card, "ความมั่นใจ", 9, FontStyle.Normal, new Color32(207, 218, 212, 255),
            new Vector2(.77f, .59f), new Vector2(.96f, .70f), TextAnchor.MiddleCenter);

        string[] nutrientNames = { "พลังงาน", "น้ำตาล", "ใยอาหาร" };
        string[] nutrientValues = { $"{selected.calories:0.#} kcal", $"{selected.sugar:0.#} g", $"{selected.fiber:0.#} g" };
        for (int i = 0; i < nutrientNames.Length; i++)
        {
            float left = .035f + i * .31f;
            var tile = Panel("Nutrition summary tile", card, GlassTileColor,
                new Vector2(left, .34f), new Vector2(left + .295f, .54f));
            AddCardText(tile, nutrientNames[i], 10, FontStyle.Normal, new Color32(210, 220, 215, 255),
                new Vector2(.04f, .51f), new Vector2(.96f, .94f), TextAnchor.MiddleCenter);
            AddCardText(tile, nutrientValues[i], 13, FontStyle.Bold, Color.white,
                new Vector2(.03f, .08f), new Vector2(.97f, .58f), TextAnchor.MiddleCenter);
        }

        AddResultAction(card, "ดูข้อมูลเพิ่มเติม", () => ShowDetail(selected),
            new Vector2(.035f, .075f), new Vector2(.48f, .275f), false, true);
        bool arModelReady = modelRegistry != null && modelRegistry.HasPrefab(selected.id);
        AddResultAction(card, arModelReady ? "View in AR" : "View in AR · ยังไม่มีโมเดล",
            () => OpenAR(selected, true), new Vector2(.52f, .075f), new Vector2(.965f, .275f), true, arModelReady);

        AddBottomUtilities(true);
    }
}
