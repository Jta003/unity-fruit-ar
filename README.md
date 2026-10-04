# Fruit Nutrition AR (Unity)

Standalone Unity Android application. This folder does not load or call the Flutter project at runtime. The Flutter source was read only to copy nutrition values, labels, and the TFLite model into this project.

## Editor and packages

- Unity Editor: `6000.3.18f1` (installed on the development machine)
- AR Foundation: `6.3.1`
- Google ARCore XR Plugin: `6.3.1`
- XR Plug-in Management: `4.5.3`
- TFLite for Unity: `com.github.asus4.tflite` `2.21.0-rc1`
- uGUI: `2.0.0`

AR Foundation and ARCore use matching 6.3 package versions. TFLite 2.21 supports Unity 6000.3 and Android ARM64. References: [Unity ARCore package compatibility](https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.xr.arcore.html) and [TFLite Unity samples/package](https://github.com/asus4/tf-lite-unity-sample).

## Open and run

1. Open this directory (`unity_fruit_starter`) from Unity Hub with Unity `6000.3.18f1` and let Package Manager resolve dependencies.
2. Confirm `Assets/Scenes/FruitNutrition.unity` is the first enabled Build Settings scene.
3. For AR on Android, open **Project Settings → XR Plug-in Management → Android** and enable **ARCore**. Use an ARCore-supported device.
4. Select Android in Build Profiles. Use ARM64 and IL2CPP (the bootstrap sets these), grant camera permission, and build an APK.
5. Press Play in the Editor to inspect library, nutrition, favorites, history, and compare flows. Camera and AR require device support.

The first package resolve requires network access. If the package manager cannot reach the npm registry, retry resolution while online.

## Project layout

- `Assets/Scenes/FruitNutrition.unity`: single-scene mobile app; screens are runtime Canvas views.
- `Assets/Scripts/Core`: fruit data, database lookup, local favorites/history.
- `Assets/Scripts/ML`: camera capture, TFLite model validation, preprocessing and inference.
- `Assets/Scripts/AR`: AR plane raycasts, anchors, placement, gestures and fruit prefab registry.
- `Assets/Scripts/UI`: mobile UI and navigation.
- `Assets/Resources/fruits.json`: nine nutrition records sourced from the reference database.
- `Assets/Resources/labels.txt`: nine model labels in output order.
- `Assets/Resources/fruit_model.bytes` and `Assets/Models/fruit_model.tflite`: copied model.

## Model contract

Reference code reports image preprocessing as centered square crop, resize to 224×224, RGB float normalization `(channel - 127.5) / 127.5`, and argmax over nine scores. Unity validates tensor shape and type at startup and refuses inference if they differ. It uses `FruitClassifier.ConfidenceThreshold` (`0.55`, matching the reference app). The actual TFLite input/output tensor metadata has not yet been extracted in this environment; Unity logs the model-reported shape/type on initialization and blocks if the assumed input contract is incorrect. Predictions are real model output; no filename matching or random score is used.

## AR and 3D assets

The app creates AR Foundation session, camera background, plane detection, raycast and anchor managers at runtime. Enable the ARCore loader in Project Settings before building. `FruitModelRegistry` maps fruit IDs to prefab assets. No licensed 3D fruit meshes or images were present in the reference assets, so no fake model is substituted: assign a model prefab for each fruit before AR placement can show fruit geometry. Gesture controls include tap to place, drag to move, pinch to scale, two-finger movement to rotate, plus Delete and Reset.

## Persistence

Favorites and up to 100 scan history records are stored locally as JSON strings in PlayerPrefs. They are device-local; no SQLite dependency or network service is used.

## Current limitations

- Unity package restore, compilation, device tests and APK build still need to be run on this machine/device.
- ARCore loader activation is a Unity Project Settings step; it is not enabled until selected for the Android target.
- Fruit prefab assets are absent. AR placement reports the missing mapping until prefab assets are assigned.
- Exact TFLite tensor metadata and on-device model predictions have not yet been verified.
- The first UI pass uses Unity uGUI built-in font; Thai glyph coverage may depend on the device font. Replace with an appropriately licensed Thai-capable TMP font for release.
