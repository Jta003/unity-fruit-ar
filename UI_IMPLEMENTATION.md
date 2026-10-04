# SVG UI implementation

Reference: user-supplied “Fruit Nutrition AR UI Design.svg”, 390 × 844 phone frames.

## UI polish tokens and artwork

Runtime screens share the palette in `FruitNutritionApp.cs`: background `#F3F1E7`, card
surface `#FFFDF6`, primary `#35B95F`, dark green `#173F35`, soft green `#DDEEDC`, text
`#18302B`, secondary text `#6E7C76`, and accent `#F4B942`. Scan and AR retain the live
camera feed as their background.

Fruit artwork is resolved centrally by `fruitId` in `FruitNutritionApp.Components.cs`; it is
independent from `FruitModelRegistry`. The resolver maps all nine catalog IDs (Apple, Banana,
Orange, Mango, Grapes, Strawberry, Cherry, Kiwi and Chickoo) to individual transparent PNG
sprites in `Assets/Resources/FruitArt`. Artwork is used consistently in Home, Result, History,
Favorites, Fruit Library, Nutrition Detail and both Compare cards. Images use Preserve Aspect;
the older `*_large` files remain in place but are no longer selected by the resolver.

| fruitId | Runtime sprite resource |
| --- | --- |
| `apple` | `fruit_apple` |
| `banana` | `fruit_banana` |
| `orange` | `fruit_orange` |
| `mango` | `fruit_mango` |
| `grapes` | `fruit_grapes` |
| `strawberry` | `fruit_strawberry` |
| `cherry` | `fruit_cherry` |
| `kiwi` | `fruit_kiwi` |
| `chickoo` | `fruit_chickoo` |

## Integration

Open the existing Assets/Scenes/FruitNutrition.unity scene and enter Play mode.
UI is composed at runtime by the existing FruitNutritionApp scene component.
No scene replacement or prefab migration is required. `FruitModelRegistry` is attached to the
existing scene root and references `Assets/Prefabs/Fruits/AppleAR.prefab` plus the registered fruit
prefabs. Each prefab wraps its source model in a `FruitAR/Model` hierarchy and normalizes its pivot
to the floor without changing the source FBX/OBJ geometry.

The UI controller is split into:

- FruitNutritionApp.cs: initialization, safe area, screen lifecycle, inference handoff, AR lifecycle.
- FruitNutritionApp.Screens.cs: home, detail and compare layouts.
- FruitNutritionApp.ScanScreen.cs: camera screen and captured result overlay.
- FruitNutritionApp.Components.cs: cards, artwork, typography, scroll containers and navigation.
- CameraImageLayout.cs: proportional cover sizing, camera rotation and vertical mirroring.
- ScanFocusGraphic.cs: dashed circular focus guide.

FruitClassifier.cs retains the existing model and confidence threshold. FruitScanService now
owns the last successful capture and its orientation. It cancels an obsolete camera startup
when navigation closes the scanner, and releases the previous successful frame on replacement.

## Screen mapping

| SVG | Runtime |
| --- | --- |
| Home | Fruit Nutrition AR, today's scan count, recent scans, scan action |
| Camera scan | Live webcam feed, focus guide, capture, history and manual fruit selection |
| Detected | Captured frame, real confidence, translucent nutrition card and detail link |
| Nutrition Detail | Fruit art, health score, energy/fiber/sugar tiles, nutrition facts, favorites, AR and compare |
| Compare | Two fruit cards, four comparison bars and a deterministic nutrition comparison note |
| Scan History | Persisted scan rows with date and health score |

Favorites and the fruit library remain reachable through the bottom navigation.
Compare preselects the fruit from the current result/detail and asks for the second fruit.

## Detection and AR

Capture runs the existing classifier. Only a recognized fruit meeting the existing confidence
threshold opens the result and enters history. Failure leaves the scanner open with feedback.
The result uses the captured texture; it is not a fabricated live recognition signal.
The scan result shows **View in AR** when the classified fruit has a prefab registered in
`FruitModelRegistry`. This uses the existing `selected` FruitData without running TFLite again.
Unregistered fruits show the existing unavailable state and cannot open a blank model-placement
screen. Kiwi and Chickoo intentionally remain unregistered while their nutrition, scan history,
favorites and compare data remain available.

Fruit AR reuses the app's AR Session, XR Origin and AR Camera. The existing AR Session also has an
ARInputManager so the Tracked Pose Driver can follow the handheld AR device pose. The XR Origin adds
plane, raycast and anchor managers for fruit placement. It accepts only upward-facing horizontal
planes. A green ring marks a detected placement point. A tap places the selected model; after placement,
a horizontal one-finger drag rotates the visual pivot and a tap moves that same model to a new anchor.
Pointers that begin on UI are ignored, and a drag cannot trigger placement. Rotation sensitivity is
0.25 degrees per horizontal screen pixel. The nutrition card appears after placement. Leaving AR removes
the model, anchor, indicator and temporary placement state. Anchors are not persisted across launches.

`Assets/Editor/FruitARPrefabBuilder.cs` creates the registered fruit wrappers, Lit materials,
Android texture import limits and scene registry entries. Run **Fruit Nutrition AR > Build
Registered Fruit AR Prefabs** in the Unity Editor after importing source assets. The validation menu
checks prefab renderers/materials and registry IDs. Kiwi and Chickoo are intentionally excluded.

## Assets and intentional limitations

`Assets/ArtSource/FruitIconSheet_Source.png` preserves the provided 1536 × 1024 source sheet.
`Assets/Resources/FruitArt` contains nine individually extracted fruit sprites and five
navigation images. The fruit sprites use Sprite (2D and UI), Single mode, a 512-pixel import
limit, no mipmaps, and no texture compression. Extraction preserves the source alpha and aspect
ratio; Cherry and Kiwi, which touch in the source sheet, were separated along their visible edge.

The Fruit Icon Sheet supplies all nine fruit images. No substitute or generated fruit pictures
were added. The source sheet is kept outside `Resources` so it is not loaded into runtime UI.

The existing UGUI Text and runtime font are retained. Exact font files were not supplied with
the outlined SVG, so typography is not a pixel-perfect reproduction.

The summary reports actual scans instead of claiming that scans are consumed portions.
Nutrition values and history come from the existing database, rather than the SVG's sample data.
The comparison note is calculated from stored values; it is not labeled as an AI-generated claim.
The flash icon from the design is not shown because the current webcam service has no torch API.
The result card uses alpha transparency; it does not perform a costly live background blur.

## Verification

Unity 6000.3.18f1 C# compilation was checked with the project's assembly references.
The isolated UiValidation project was run in Unity Play mode and rendered at 390 × 844.
Screenshots include home, detail, compare, history, favorites and a result layout fixture.
The fixture uses an artwork texture to inspect layout; it does not verify camera inference.
See UIReview for delivered screenshots and the final build report when available.

On-device permission handling, live capture, AR camera handoff, plane detection, Apple model scale
and orientation should be checked on the target phone after installing the rebuilt APK. Editor
screenshots and compilation cannot verify AR tracking or placement on the phone.
