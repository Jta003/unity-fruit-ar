using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public sealed partial class FruitNutritionApp : MonoBehaviour
{
    [SerializeField] private FruitModelRegistry modelRegistry;
    private enum Page { Home, Scan, Result, Detail, Fruits, History, Favorites, Compare, AR }
    private static readonly Color CanvasColor = new Color32(243, 241, 231, 255);
    private static readonly Color InkColor = new Color32(24, 48, 43, 255);
    private static readonly Color MutedColor = new Color32(110, 124, 118, 255);
    private static readonly Color PrimaryColor = new Color32(53, 185, 95, 255);
    private static readonly Color DarkGreenColor = new Color32(23, 63, 53, 255);
    private static readonly Color SoftGreenColor = new Color32(221, 238, 220, 255);
    private static readonly Color SurfaceColor = new Color32(255, 253, 246, 255);
    private static readonly Color AccentColor = new Color32(244, 185, 66, 255);
    private static readonly Color BorderColor = new Color32(222, 228, 218, 255);
    private static Sprite roundedSprite;
    private static readonly Dictionary<string, Sprite> artSprites = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, string> fruitArtResourceById = new Dictionary<string, string>
    {
        { "apple", "fruit_apple" },
        { "banana", "fruit_banana" },
        { "orange", "fruit_orange" },
        { "mango", "fruit_mango" },
        { "grapes", "fruit_grapes" },
        { "strawberry", "fruit_strawberry" },
        { "cherry", "fruit_cherry" },
        { "kiwi", "fruit_kiwi" },
        { "chickoo", "fruit_chickoo" }
    };
    private static readonly HashSet<string> missingFruitArtLogged = new HashSet<string>();
    private Canvas canvas;
    private RectTransform safeArea;
    private RectTransform body;
    private Image backgroundPanel;
    private Image arHeaderPanel;
    private RectTransform navigationBar;
    private readonly List<Image> navigationIcons = new List<Image>();
    private readonly List<Text> navigationLabels = new List<Text>();
    private Text headerLabel;
    private Text status;
    private RawImage preview;
    private Button captureButton;
    private RectTransform previewViewport;
    private FruitData selected;
    private FruitData compareA, compareB;
    private float confidence;
    private bool openArAfterCapture;
    private Page page;
    private ARSession arSession;
    private GameObject arRoot;
    private ARPlacementController arPlacement;
    private GameObject arNutritionPanel;
    private RectTransform scrollContent;
    private int pageRevision;

    private void Start()
    {
        if (modelRegistry == null) modelRegistry = GetComponent<FruitModelRegistry>();
        if (FruitDatabase.Instance == null) new GameObject("FruitDatabase").AddComponent<FruitDatabase>();
        if (FruitScanService.Instance == null) new GameObject("FruitScanService").AddComponent<FruitScanService>();
        BuildCanvas(); ShowHome();
    }

    private void BuildCanvas()
    {
        var uiCameraObject = new GameObject("UI Background Camera", typeof(Camera));
        uiCameraObject.transform.SetParent(transform, false);
        var uiCamera = uiCameraObject.GetComponent<Camera>();
        uiCamera.cullingMask = 0; uiCamera.depth = -100;
        uiCamera.clearFlags = CameraClearFlags.SolidColor; uiCamera.backgroundColor = CanvasColor;
        var canvasObject = new GameObject("Application UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null) eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var legacyInput = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyInput != null) { legacyInput.enabled = false; Destroy(legacyInput); }
        if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        if (eventSystem.GetComponent<BaseInputModule>() == null) eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
        canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(390, 844); scaler.matchWidthOrHeight = .5f;
        var root = MakeRect("Background", canvas.transform); root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = Vector2.zero; root.offsetMax = Vector2.zero;
        backgroundPanel = root.gameObject.AddComponent<Image>();
        backgroundPanel.color = CanvasColor;
        safeArea = MakeRect("Safe Area", root);
        ApplySafeArea();
        var topBar = MakeRect("AR header backdrop", safeArea); topBar.anchorMin = new Vector2(0, .855f); topBar.anchorMax = Vector2.one; topBar.offsetMin = Vector2.zero; topBar.offsetMax = Vector2.zero;
        arHeaderPanel = topBar.gameObject.AddComponent<Image>(); arHeaderPanel.color = new Color(DarkGreenColor.r, DarkGreenColor.g, DarkGreenColor.b, .9f); arHeaderPanel.enabled = false; arHeaderPanel.raycastTarget = false;
        headerLabel = MakeText("Fruit Nutrition", safeArea, 24, FontStyle.Bold, new Vector2(.06f, .92f), new Vector2(.94f, .99f), TextAnchor.MiddleLeft);
        headerLabel.color = InkColor;
        status = MakeText("", safeArea, 13, FontStyle.Normal, new Vector2(.06f, .875f), new Vector2(.94f, .925f), TextAnchor.MiddleLeft);
        status.color = MutedColor;
        body = MakeRect("Page", safeArea); body.anchorMin = new Vector2(.045f, .105f); body.anchorMax = new Vector2(.955f, .86f); body.offsetMin = Vector2.zero; body.offsetMax = Vector2.zero;
        BuildNavigation(safeArea);
    }

    private void ApplySafeArea()
    {
        if (safeArea == null) return;
        var area = Screen.safeArea;
        safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        ApplySafeArea();
        if (page != Page.AR || arPlacement == null) return;
        status.text = arPlacement.Status;
        if (arNutritionPanel != null) arNutritionPanel.SetActive(arPlacement.HasPlacement);
    }

    private void ClearPage()
    {
        pageRevision++;
        status.transform.SetParent(safeArea, false);
        status.rectTransform.anchorMin = new Vector2(.06f,.875f); status.rectTransform.anchorMax = new Vector2(.94f,.925f);
        status.rectTransform.offsetMin = status.rectTransform.offsetMax = Vector2.zero;
        status.alignment = TextAnchor.MiddleLeft;
        foreach (Transform child in body) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        foreach (var item in pageDecorations) { if (item != null) { item.SetActive(false); Destroy(item); } }
        pageDecorations.Clear();
        body.anchorMin = new Vector2(.045f, .105f); body.anchorMax = new Vector2(.955f, .86f);
        headerLabel.gameObject.SetActive(true); status.gameObject.SetActive(true);
        headerLabel.color = page == Page.AR ? Color.white : InkColor; status.color = page == Page.AR ? Color.white : MutedColor;
        backgroundPanel.color = CanvasColor;
        if (previewViewport != null && previewViewport.parent == safeArea) { previewViewport.gameObject.SetActive(false); Destroy(previewViewport.gameObject); }
        if (page != Page.AR && backgroundPanel != null) backgroundPanel.enabled = true;
        preview = null;
        captureButton = null;
        previewViewport = null;
        scrollContent = null;
    }

    private void OpenScan() => OpenScanPage(false);
    private void OpenARScan() => OpenScanPage(true);
    private IEnumerator StartCamera()
    {
        var revision = pageRevision;
        var task = FruitScanService.Instance.StartCamera();
        while (!task.IsCompleted) yield return null;
        if (revision != pageRevision || page != Page.Scan || preview == null) yield break;
        if (task.IsFaulted || task.IsCanceled || !task.Result)
        { status.text = "เปิดกล้องไม่ได้ ตรวจสิทธิ์กล้องในการตั้งค่า หรือเลือกผลไม้เอง"; yield break; }
        captureButton.interactable = true;
        preview.texture = FruitScanService.Instance.CameraFeed;
        var layout = preview.gameObject.AddComponent<CameraImageLayout>();
        status.text = "วางผลไม้หนึ่งชิ้นไว้กลางภาพ แล้วแตะ Capture";
        while (revision == pageRevision && page == Page.Scan && preview != null)
        {
            var feed = FruitScanService.Instance.CameraTexture;
            if (feed != null) { layout.Rotation = feed.videoRotationAngle; layout.Mirrored = feed.videoVerticallyMirrored; }
            yield return null;
        }
    }
    private void Capture()
    {
        var result = FruitScanService.Instance.CaptureAndClassify(); confidence = result.confidence;
        if (result.fruit == null) { status.text = result.error + $" · {confidence:P0} กรุณาลองใหม่"; return; }
        selected = result.fruit;
        if (openArAfterCapture) { openArAfterCapture = false; OpenAR(selected); } else ShowResult();
    }
    private void ShowList(Page kind)
    {
        ExitCameraMode(); page = kind; ClearPage(); EnableScrollableButtons(); SetNavigationVisible(true);
        StandardHeader(kind == Page.History ? "Scan History" : kind == Page.Favorites ? "Favorites" : "Fruit Library",
            kind == Page.History ? "รายการผลไม้ที่คุณเคยสแกน" : "เลือกผลไม้เพื่อดูข้อมูลโภชนาการ");
        if (kind == Page.History)
        {
            var history = LocalRepository.GetHistory();
            if (history.Count == 0) AddText("ยังไม่มีประวัติการสแกน");
            foreach (var record in history)
            {
                var fruit = FruitDatabase.Instance.Find(record.fruitId);
                if (fruit != null) AddFruitRow(fruit, $"{record.scannedAt} · {record.confidence:P0}", () => ShowDetail(fruit));
            }
        }
        else
        {
            var fruits = FruitDatabase.Instance.All.Where(f => kind != Page.Favorites || LocalRepository.IsFavorite(f.id)).ToArray();
            if (fruits.Length == 0) AddText("ยังไม่มีรายการโปรด\nบันทึกผลไม้ได้จากหน้ารายละเอียด");
            foreach (var fruit in fruits) AddFruitRow(fruit, $"{fruit.calories:0.#} kcal", () => ShowDetail(fruit));
        }
    }
    private void BeginCompare() { compareA = selected; compareB = null; ShowCompare(); }

    private void OpenAR(FruitData fruit, bool returnToResult = false)
    {
        if (fruit == null) { ShowList(Page.Fruits); return; }
        if (modelRegistry != null && modelRegistry.HasPrefab(fruit.id))
        {
            ShowFruitAR(fruit, returnToResult);
            return;
        }
        ExitCameraMode(); selected = fruit; page = Page.AR; ClearPage(); SetNavigationVisible(false);
        StandardHeader("AR Nutrition", "กล้องจริง · ข้อมูลโภชนาการซ้อนบนภาพ");
        backgroundPanel.enabled = false; arHeaderPanel.enabled = true;
        headerLabel.color = status.color = Color.white;
        EnableScrollableButtons();
#if UNITY_EDITOR
        backgroundPanel.enabled = true;
        status.text = "เปิด AR บนอุปกรณ์ Android ที่รองรับ ARCore";
#else
        BuildAR();
#endif
        AddHeading(fruit.englishName);
        AddCompactNutrition(fruit);
        AddButton("สแกนผลไม้อีกชิ้น", OpenARScan, primary: true);
        AddButton("กลับข้อมูลผลไม้", () => ShowDetail(fruit));
        AddButton("หน้าแรก", ShowHome);
    }
    private void BuildAR(FruitData placementFruit = null)
    {
        arRoot = new GameObject("AR Session Origin");
        if (arSession == null) arSession = FindFirstObjectByType<ARSession>();
        if (arSession == null)
            arSession = new GameObject("AR Session", typeof(ARSession), typeof(ARInputManager)).GetComponent<ARSession>();
        if (arSession.GetComponent<ARInputManager>() == null)
            arSession.gameObject.AddComponent<ARInputManager>();
        arSession.enabled = true;
        var cameraObject = new GameObject("AR Camera", typeof(Camera), typeof(ARCameraManager), typeof(ARCameraBackground));
        cameraObject.tag = "MainCamera";
        if (placementFruit != null)
        {
            // This scene has no light. The imported Apple uses a lit material, so
            // give the AR model a camera-facing light without altering its FBX.
            var lightObject = new GameObject("AR Fruit Light", typeof(Light));
            lightObject.transform.SetParent(cameraObject.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(20f, -25f, 0f);
            var fruitLight = lightObject.GetComponent<Light>();
            fruitLight.type = LightType.Directional;
            fruitLight.intensity = 1.5f;
            fruitLight.shadows = LightShadows.None;
        }
        var driver = cameraObject.AddComponent<TrackedPoseDriver>();
        driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        var positionAction = new InputAction("AR Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
        positionAction.AddBinding("<HandheldARInputDevice>/devicePosition");
        var rotationAction = new InputAction("AR Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
        rotationAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
        driver.positionInput = new InputActionProperty(positionAction);
        driver.rotationInput = new InputActionProperty(rotationAction);
        var offset = new GameObject("Camera Floor Offset"); offset.transform.SetParent(arRoot.transform, false);
        cameraObject.transform.SetParent(offset.transform, false);
        var origin = arRoot.AddComponent<XROrigin>(); origin.CameraFloorOffsetObject = offset; origin.Camera = cameraObject.GetComponent<Camera>();
        if (placementFruit == null) return;

        arRoot.AddComponent<ARPlaneManager>();
        arRoot.AddComponent<ARRaycastManager>();
        arRoot.AddComponent<ARAnchorManager>();
        arPlacement = arRoot.AddComponent<ARPlacementController>();
        arPlacement.Configure(CreatePlacementIndicator(arRoot.transform), modelRegistry);
        if (arPlacement.Initialize(origin)) arPlacement.SelectFruit(placementFruit);
    }
    private void ExitCameraMode()
    {
        if (arRoot != null) { arRoot.SetActive(false); Destroy(arRoot); arRoot = null; }
        arPlacement = null;
        arNutritionPanel = null;
        if (arSession != null) arSession.enabled = false;
        if (arHeaderPanel != null) { arHeaderPanel.enabled = false; arHeaderPanel.raycastTarget = false; }
        if (FruitScanService.Instance != null) FruitScanService.Instance.StopCamera();
    }
    private void OnDestroy()
    {
        ExitCameraMode();
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
