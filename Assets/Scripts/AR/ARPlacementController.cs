using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public sealed class ARPlacementController : MonoBehaviour
{
    [SerializeField] private GameObject placementIndicator;
    [SerializeField] private FruitModelRegistry modelRegistry;
    [SerializeField, Min(0f)] private float rotationSensitivity = 0.25f;
    [SerializeField, Min(1f)] private float dragThresholdPixels = 12f;

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private ARAnchorManager anchorManager;
    private ARAnchor activeAnchor;
    private GameObject activeFruit;
    private Transform activeVisualRoot;
    private FruitData selectedFruit;
    private bool poseFound;
    private bool placementPending;
    private int operationVersion;
    private int activeFingerId = -1;
    private bool pointerGestureActive;
    private bool pointerStartedOnUI;
    private bool pointerHadPlacement;
    private bool pointerMoved;
    private Vector2 pointerStartPosition;
    private Vector2 previousPointerPosition;

    private const int MousePointerId = -1;

    private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();
    private static readonly List<RaycastResult> UiHits = new List<RaycastResult>();

    public string Status { get; private set; } = "Move your phone to detect a surface";
    public bool HasPlacement => activeFruit != null;

    public void Configure(GameObject indicator, FruitModelRegistry registry)
    {
        placementIndicator = indicator;
        modelRegistry = registry;
        if (placementIndicator != null) placementIndicator.SetActive(false);
    }

    public bool Initialize(XROrigin origin)
    {
        if (origin == null)
        {
            Status = "AR setup is unavailable";
            return false;
        }

        raycastManager = origin.GetComponent<ARRaycastManager>();
        planeManager = origin.GetComponent<ARPlaneManager>();
        anchorManager = origin.GetComponent<ARAnchorManager>();
        if (raycastManager == null || planeManager == null || anchorManager == null || modelRegistry == null)
        {
            Status = "AR placement is unavailable";
            return false;
        }

        planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        Status = "Move your phone to detect a surface";
        return true;
    }

    public void SelectFruit(FruitData fruit)
    {
        if (selectedFruit != null && fruit != null && selectedFruit.id != fruit.id) DeletePlacement();
        selectedFruit = fruit;
        if (fruit == null || modelRegistry == null || !modelRegistry.HasPrefab(fruit.id))
            Status = "3D model is not available";
    }

    private void Update()
    {
        if (raycastManager == null || planeManager == null || anchorManager == null || selectedFruit == null) return;

        poseFound = TryGetPlanePose(new Vector2(Screen.width * .5f, Screen.height * .5f), out var centerPose);
        if (placementIndicator != null)
        {
            placementIndicator.SetActive(poseFound && !placementPending);
            if (poseFound) placementIndicator.transform.SetPositionAndRotation(centerPose.position, centerPose.rotation);
        }

        if (!HasPlacement && !placementPending)
            Status = poseFound ? "Tap a surface to place the model" : "Move your phone to detect a surface";

        if (Input.touchCount > 0)
        {
            HandleTouchInput();
            return;
        }

#if UNITY_EDITOR
        HandleMouseInput();
#endif
    }

    private void HandleTouchInput()
    {
        // Keep one active finger for predictable rotation; ignore additional fingers.
        if (!pointerGestureActive)
        {
            var first = Input.GetTouch(0);
            if (first.phase != TouchPhase.Began) return;
            BeginPointer(first.fingerId, first.position);
        }

        for (var i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            if (touch.fingerId != activeFingerId) continue;

            switch (touch.phase)
            {
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    MovePointer(touch.position);
                    break;
                case TouchPhase.Ended:
                    MovePointer(touch.position);
                    EndPointer(touch.position);
                    break;
                case TouchPhase.Canceled:
                    ResetPointerGesture();
                    break;
            }
            return;
        }
    }

#if UNITY_EDITOR
    private void HandleMouseInput()
    {
        var position = (Vector2)Input.mousePosition;
        if (Input.GetMouseButtonDown(0)) BeginPointer(MousePointerId, position);
        if (!pointerGestureActive || activeFingerId != MousePointerId) return;
        if (Input.GetMouseButton(0)) MovePointer(position);
        if (Input.GetMouseButtonUp(0)) EndPointer(position);
    }
#endif

    private void BeginPointer(int pointerId, Vector2 position)
    {
        pointerGestureActive = true;
        activeFingerId = pointerId;
        pointerStartedOnUI = IsPointerOnUI(position, pointerId);
        pointerHadPlacement = HasPlacement;
        pointerMoved = false;
        pointerStartPosition = previousPointerPosition = position;
    }

    private void MovePointer(Vector2 position)
    {
        if (!pointerGestureActive) return;

        var horizontalDelta = position.x - previousPointerPosition.x;
        previousPointerPosition = position;
        if (pointerStartedOnUI) return;

        if (!pointerMoved && Vector2.Distance(pointerStartPosition, position) >= dragThresholdPixels)
            pointerMoved = true;

        // A drag that began with a placed fruit is reserved for rotation, never placement.
        if (pointerHadPlacement && pointerMoved && activeVisualRoot != null)
            activeVisualRoot.localRotation = Quaternion.AngleAxis(horizontalDelta * rotationSensitivity, Vector3.up)
                                             * activeVisualRoot.localRotation;
    }

    private void EndPointer(Vector2 position)
    {
        var shouldPlace = !pointerStartedOnUI && !pointerMoved;
        ResetPointerGesture();
        if (shouldPlace && TryGetPlanePose(position, out var touchPose)) PlaceSelected(touchPose);
    }

    private void ResetPointerGesture()
    {
        pointerGestureActive = false;
        activeFingerId = -1;
        pointerStartedOnUI = false;
        pointerHadPlacement = false;
        pointerMoved = false;
    }

    private bool TryGetPlanePose(Vector2 screenPosition, out Pose pose)
    {
        pose = default;
        if (raycastManager == null || planeManager == null ||
            !raycastManager.Raycast(screenPosition, Hits, TrackableType.PlaneWithinPolygon)) return false;

        foreach (var hit in Hits)
        {
            var plane = planeManager.GetPlane(hit.trackableId);
            if (plane == null || plane.alignment != PlaneAlignment.HorizontalUp) continue;
            pose = hit.pose;
            return true;
        }
        return false;
    }

    private static bool IsPointerOnUI(Vector2 position, int pointerId)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null) return false;
        if (eventSystem.IsPointerOverGameObject(pointerId)) return true;
        UiHits.Clear();
        eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = position }, UiHits);
        return UiHits.Count > 0;
    }

    private async void PlaceSelected(Pose pose)
    {
        if (!isActiveAndEnabled || selectedFruit == null || !modelRegistry.HasPrefab(selectedFruit.id) ||
            anchorManager == null || placementPending) return;

        placementPending = true;
        var version = operationVersion;
        ARAnchor newAnchor = null;
        Status = "Placing model...";
        try
        {
            var result = await anchorManager.TryAddAnchorAsync(pose);
            newAnchor = result.value;
            if (version != operationVersion || !isActiveAndEnabled)
            {
                if (newAnchor != null) Destroy(newAnchor.gameObject);
                return;
            }
            if (newAnchor == null)
            {
                Status = "Could not place the model. Tap again.";
                return;
            }

            if (activeFruit == null)
            {
                activeFruit = modelRegistry.Create(selectedFruit.id, newAnchor.transform);
                if (activeFruit == null)
                {
                    Destroy(newAnchor.gameObject);
                    Status = "3D model is not available";
                    return;
                }
                activeVisualRoot = CreateVisualRotationPivot(activeFruit.transform);
                if (activeVisualRoot == null)
                    Debug.LogWarning("FRUIT_AR_NO_VISUAL_ROOT: placed model has no child visual transform to rotate.");
            }
            else
            {
                activeFruit.transform.SetParent(newAnchor.transform, false);
            }

            activeFruit.transform.localPosition = Vector3.zero;
            activeFruit.transform.localRotation = Quaternion.identity;
            var renderers = activeFruit.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Destroy(activeFruit);
                activeFruit = null;
                Destroy(newAnchor.gameObject);
                Status = "Fruit model has no visible mesh";
                Debug.LogError("FRUIT_AR_NO_RENDERER: registered fruit prefab contains no Renderer");
                return;
            }
            var oldAnchor = activeAnchor;
            activeAnchor = newAnchor;
            if (oldAnchor != null) Destroy(oldAnchor.gameObject);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            var arCamera = Camera.main;
            var projectedCenter = arCamera != null ? arCamera.WorldToScreenPoint(bounds.center) : Vector3.negativeInfinity;
            var materialInfo = renderers[0].sharedMaterial != null
                ? $"{renderers[0].sharedMaterial.shader.name},supported={renderers[0].sharedMaterial.shader.isSupported}"
                : "missing";
            Debug.Log($"FRUIT_AR_PLACED fruitId={selectedFruit.id} renderers={renderers.Length} bounds={bounds.size} anchor={activeAnchor.transform.position} camera={arCamera?.transform.position} cameraRotation={arCamera?.transform.eulerAngles} forward={arCamera?.transform.forward} screenCenter={projectedCenter} rendererVisible={renderers[0].isVisible} enabled={renderers[0].enabled} material={materialInfo}");
            Status = selectedFruit.englishName + " placed · Drag left or right to rotate · Tap to move";
        }
        catch (Exception exception)
        {
            if (newAnchor != null && newAnchor != activeAnchor) Destroy(newAnchor.gameObject);
            if (version == operationVersion)
            {
                Status = "Could not place the model. Tap again.";
                Debug.LogException(exception);
            }
        }
        finally
        {
            if (version == operationVersion) placementPending = false;
        }
    }

    public void DeletePlacement()
    {
        operationVersion++;
        placementPending = false;
        ResetPointerGesture();
        if (activeFruit != null) Destroy(activeFruit);
        if (activeAnchor != null) Destroy(activeAnchor.gameObject);
        activeFruit = null;
        activeVisualRoot = null;
        activeAnchor = null;
        Status = "Move your phone to detect a surface";
    }

    private static Transform CreateVisualRotationPivot(Transform placementRoot)
    {
        if (placementRoot == null) return null;
        var visualChildren = new List<Transform>();
        for (var i = 0; i < placementRoot.childCount; i++)
        {
            var child = placementRoot.GetChild(i);
            if (child.GetComponentsInChildren<Renderer>(true).Length > 0) visualChildren.Add(child);
        }
        if (visualChildren.Count == 0) return null;

        // The imported model child can have an offset pivot to sit on the plane.
        // Rotate a zeroed pivot at the placement origin so the fruit turns in place.
        var pivotObject = new GameObject("Fruit Visual Rotation Pivot");
        var pivot = pivotObject.transform;
        pivot.SetParent(placementRoot, false);
        foreach (var child in visualChildren) child.SetParent(pivot, false);
        return pivot;
    }

    private void OnDisable()
    {
        DeletePlacement();
        poseFound = false;
        if (placementIndicator != null) placementIndicator.SetActive(false);
    }
}
