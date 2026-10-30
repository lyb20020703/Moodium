# Moodium（Unity / visionOS）修复计划：开场按钮、按钮底色、世界定位与 Moodi 挤出

> 交给 Codex 执行。基于 `lyb20020703/Moodium` 的 `main` 分支最新提交 `d9c4093e`（2026-10-30 12:19，"Integrate Moodi squeeze-out rescue and bubble point-cloud motion"）逐行阅读后写成。
> 改动按 P0 → P2 排序。**每一步做完单独提交**，提交信息里写清楚是哪一步，这样任何一步在真机上不对都能单独回退。

---

## 0. 总览：现在的问题和原因

| # | 用户看到的问题 | 根本原因（有代码证据） | 本计划的处理 |
|---|---|---|---|
| A | 进世界后，软模型和贴纸、尺寸调节框**对不上，位置偏、整体偏小** | `d9c4093e` 把 `Assets/prefab/CandySoft.prefab`、`NatureSoft.prefab` 里的模型子节点从原模型换成了开洞的 `Assets/Model/Soft/*Soft_Mouth.fbx`。`CalibratedSensoryBagPresentation.FitVisualToCalibration()` 是用 **prefab 下所有 Renderer 的包围盒** 来缩放、居中的（`CalculateLocalRendererBounds`）；新模型里多了两个 `SkinnedMeshRenderer`（洞口补丁、喉管）、一个缩放 1/18.33 的根空物体和 25 根骨骼。蒙皮网格的 `localBounds` 是相对**根骨骼**而不是 Renderer 自身计算的，算出的包围盒比真实软模型大、中心也偏，于是 `VisualRoot` 被缩小、中心偏移。Codex 中途跑全套测试时，`CalibratedSensoryBagPresentationTests.VisualFit_MapsFrameWidthAndHeightOntoPrefabTablePlaneXAndZ` 和 `VisualRoot_FollowsStickerOrientation_WhileFittingBagXZ` 两项已经失败（`docs/verification/moodi-emergence/initial-full-suite-failures.txt`），但最后只报告了"相关套件 94/94"，这两项不在里面 | **把两个软模型 prefab 恢复到 `d9c4093e` 之前的版本**（第 3 节） |
| B | 捏软模型和进度条的联动消失，**进度一直是 0**；开头 Moodi 掉进软模型的动画也没了 | 同一个原因引出的连锁问题：<br>① `MoodiumRuntimeObjectSetup.EnsureCollider()` 按同一组 Renderer 包围盒给软模型生成 `BoxCollider`。包围盒错了，碰撞体就和看到的模型对不上，凝视 + 捏合的射线打不到它，`ChocolateCapsuleInteraction.BeginSqueeze()` 不会触发，`AnyCapsulePinched` → `CandyWorldManager.OnCapsulePinched` → `m_Energy.RegisterPinch()` 这条链就断了。<br>② `ChocolateCapsuleInteraction.CaptureBurstCenter()`（按压中心）、`SoftTouchDeformationController`（按压凹陷，取第一个 `MeshFilter`）也都是按原模型的单网格结构写的。<br>③ 新的显示层 `MoodiEmergenceDisplay.AttachMoodi()` 把救援 Moodi **改挂到 `Mouth_Rig` 下面**，再用矩阵求逆去抵消 FBX 的 1/18.33 和 0.01 两层缩放；软模型本身的尺寸已经错了，Moodi 的位置和大小就跟着错，"掉进去"的过程看不到，挤出的 Moodi 也找不到 | 恢复 prefab 后 ①② 自动恢复；③ 整套换成第 4 节的"只用 Moodi 骨骼动画"方案，**不再改挂 Moodi，也不再改软模型** |
| C | 挤压出来的不是 Moodi，而是软模型上破了个洞 | 开洞方案本身：洞口在近距离下不好看，也容易穿帮（用户已决定放弃） | 第 4 节：软模型保持原样，Moodi 叠在原来透明视频的位置，随进度从软模型里一点点升高 |
| D | 开场动画的按钮点不动，位置和边距要优化 | 见第 1 节 | 第 1 节 |
| E | 各种图片按钮在注视、点击时露出背后的半透明底色 | 见第 2 节 | 第 2 节 |

另外两处顺带要处理：
- **`Strain` 叠加层和额外上移重复了**：`Moodi_Rigged.fbx` 的 `Strain` 动画已经包含按住时上移 0.26，`MoodiEmergenceDisplay.LateUpdate()` 又额外加了 `m_PressLift = .26`，按住时 Moodi 会被抬高两倍。新方案里**不加额外上移**，只在回弹时下沉（第 4 节）。上一版交付包里的执行方案在这一点上写错了，已经更正。
- `Assets/prefab/Moodi meshy.prefab` 在 `d9c4093e` 里被换成了新的模型实例（缩放 10、旋转改变），对应的 `MoodiBreathingFloatTests`（`CandyWorldUI_MoodiMeshy_HasBreathingFloat` 等）中途也失败了。用户这次没提，但第 5 节验收时要顺便看选关轮播里的 Moodi 是否正常。

---

## 1. 开场按钮点不动、位置和边距（P0）

### 1.1 原因

**按钮被开场视频自己的碰撞盒挡住了（`d9c4093e` 引入，确定）。**

- 输入链路：`OpeningVideoSpatialUIInputManager.Update`（第 50–61 行）读 `SpatialPointerDevice` 的 `targetObject`，在它身上找 `OpeningVideoSpatialButton`。没有层过滤，RealityKit 的射线**先打到哪个碰撞体就是哪个**。
- `OpeningManager.cs:219-221` 给视频画面加了一个触发碰撞盒：`size = (1, 1, 0.04)`。视频画面的缩放是 `(1.28, 0.72, 1)`（`OpeningVideoDisplayPlacement.cs:30`），所以这个盒子在世界里是 1.28 × 0.72 m，**前表面在视频前方 2.0 cm**，而且整个开场期间都不关。
- `d9c4093e` 把按钮的位置从视频下沿以下 4 cm（`- display.up*.04f`）挪到了视频下沿以上 4 cm（`+ display.up*.04f`），离视频只有 0.3 cm（`- display.forward*.003f`）。按钮的碰撞体（前表面 0.8 cm）**整个落在视频碰撞盒里面**，射线先进视频的盒子，`targetObject` 是视频画面，按下被丢掉。真机上的表现：注视按钮没有高亮，日志里也没有 `Video language selected by spatial pointer`。
- `d9c4093e` 之前也有问题：从 `26bb73d5` 起，自定义的窗口移动条（`MoodiumAppFlowController.cs:1401-1433`，碰撞体 0.294 × 0.037 m，前表面 2.45 cm）挡住了每个按钮大约 76% 的宽度、61% 的高度，只有边上能点。这个 App 用的是 Unbounded 空间，没有系统窗口条，这个自定义移动条是唯一的"移动条"。

### 1.2 改法

**① 把按钮放到视频碰撞盒前面，拉开间距、加大点击区**（`OpeningVideoLanguageChoice.cs:65`、`OpeningVideoHardwareChoice.cs:67`，两处一样）：

```csharp
// 旧
? display.TransformPoint(new Vector3(0,-.5f,0))+display.right*(position.x<0?-.1f:.1f)+display.up*.04f-display.forward*.003f
// 新
? display.TransformPoint(new Vector3(0,-.5f,0))+display.right*(position.x<0?-.13f:.13f)+display.up*.06f-display.forward*.03f
```

碰撞体（`LanguageChoice.cs:72`、`HardwareChoice.cs:74`）：

```csharp
// 旧
item.AddComponent<BoxCollider>().size=new Vector3(.178f,.061f,.01f);
// 新
item.AddComponent<BoxCollider>().size=new Vector3(.20f,.08f,.01f);
```

两处的注释一并更新。

**② 开场视频的碰撞盒用完就关掉**（`OpeningManager.cs`，`if (videoInteraction.Activated || noInteractionElapsed >= 30f)` 里，`gate.Activate();` 之后，约第 312 行）：

```csharp
videoCollider.enabled = false;   // 开场捏合已经完成，不要再挡住选择按钮和之后的界面
```

`OpeningVideoInteraction` 激活后就不再检测，其他地方也没有用到这个碰撞盒。只做 ① 就能修好；② 防止它以后再挡住视频上的其他界面。

**③（可选）** 两个脚本里 `CreateText` 生成的问题文字加上 `raycastTarget = false`。

另外，第 2 节里这两处的 `VisionOSHoverEffect` Highlight 也要换掉（`GazeHover`），两节一起改。

### 1.3 布局数值（世界坐标，视频 1.28 × 0.72 m，离人 1.35 m）

| 项目 | 现在 | 建议 |
|---|---|---|
| 按钮中心高度（相对视频中心） | −0.32 m（视频下沿以上 4 cm） | **−0.30 m**（下沿以上 6 cm） |
| 两个按钮的中心 | ±0.10 m | **±0.13 m** |
| 离视频的距离（朝向用户） | 0.003 m（在视频碰撞盒里） | **0.03 m** |
| 两个胶囊之间的空隙 | 2.2 cm | **8.2 cm** |
| 碰撞体 | 0.178 × 0.061 m | **0.20 × 0.08 m** |
| 碰撞体之间的空隙 | 2.2 cm | **6 cm** |

改完后：
- 胶囊下沿在视频下沿以内 2.9 cm；
- 碰撞体下沿离移动条碰撞体上沿 3 cm，离移动条图形 4.8 cm；
- 两个按钮合计 0.438 m 宽，居中，不挡视频中间的主体，也不碰左右两边的手势引导（x = ±0.67 m）；
- 8 cm 高的点击区在 1.35 m 外满足 visionOS 不小于 60 pt 的建议。

如果视频底部有角色站着、按钮会挡住它，也可以把按钮放到视频**下方**、移动条两侧：中心 y = −0.40 m，x = ±0.26 m，z = 0.03 m。

### 1.4 验收

- 编辑器测试：两组按钮的碰撞体世界包围盒在视频碰撞盒和移动条碰撞体之外，并且离用户更近。
- 真机：中文 / English、有 / 没有四个按钮都能一次点中；注视有反馈（第 2 节）；按下后流程继续；窗口移动条仍然能拖动。

---

## 2. 图片按钮注视 / 点击时露出半透明底色（P1）

### 2.1 原因

露出来的不是哪张底图，而是 **visionOS 系统的注视高亮**：
- 这些按钮都在显示图片的同一个物体上挂了 `VisionOSHoverEffect`，类型是 `EffectType.Highlight`。
- 而所有按钮图片的精灵网格都是整块矩形：各个贴图后处理脚本都设了 `spriteMeshType = SpriteMeshType.FullRect`。
- RealityKit 的 Highlight 会点亮**整块网格**，不看贴图的透明度，所以 PNG 四周透明的留白也被点亮，变成一块半透明的矩形底板。捏合时高亮不消失，所以按下时也看得到。

已排除的可能：
- **另有底图？** 没有：每个按钮只有一个 `SpriteRenderer`，`opening_btn_bg_*` 在代码里没被引用。
- **UI.Button 的变色？** 开场按钮已经设了 `Selectable.Transition.None`。
- **图片本身的问题？** PNG 四角的 alpha 都是 0。

另一个相关问题：代码里"注视时换成 `_gaze` 图"的逻辑都包在 `#if UNITY_EDITOR` 里，**真机上从来不会执行**（visionOS 不允许 App 读取视线）。涉及：
- `OpeningChoiceSpriteFeedback.cs:11-19`
- `UIRefreshVisuals.cs` 里的 `UIRefreshSpriteButton.SetHovered`
- `CalibrationRotateDial.cs:35-37`

所以真机上注视时唯一的反馈就是那块矩形高亮。

问题从提交 `26bb73d5` 开始出现：开场按钮从 Canvas `Image` 改成精灵并加上了 Highlight，同一个提交新增了 `UIRefreshVisuals.Button()`（统一加 Highlight）、`CalibrationRotateDial` 和 `UIRefreshAssetPostprocessor`（FullRect）。

涉及的按钮：

| 按钮 | 加 Highlight 的位置 | 高亮区域和可见图形对比 |
|---|---|---|
| 开场语言（中文 / English） | `Opening/OpeningVideoLanguageChoice.cs:73` | 0.268 × 0.152 m 的矩形，包着 0.178 × 0.061 m 的胶囊 |
| 有没有感官袋（有 / 没有） | `Opening/OpeningVideoHardwareChoice.cs:75` | 同上 |
| 设置齿轮 | `UI/UIRefreshVisuals.cs:60`（`Button()`，`Gear()` 调用） | 0.084 m 的方块，包着约 0.046 m 的圆 |
| 设置面板的关闭和四个选项 | `SelectionSettingsVisual.cs:21,33`（经 `UIRefreshVisuals.Button`） | 文字图片只占 9%，几乎整块都是底板 |
| 校准框旋转旋钮 | `UI/CalibrationRotateDial.cs:20` | 0.081 m 的方块，包着圆形旋钮 |
| 手腕能量的圆形按钮 | `CandyEnergyWristUI.cs:753` | UGUI `Image`（Simple 模式，永远是方块） |
| 选关的 ‹ › 箭头 | `Flow/SensoryBagBranchUI.cs:36` | TMP 文字，每个字形都是方块 |
| 同样的写法 | `Guide/WorldGuideCards.cs:324`、`Calibration/SensoryBagCalibrator.cs:418`、`UI/MoodEntryChip.cs:41`、`UI/GlassSlideToEnter.cs:411` | — |

### 2.2 修法：让系统注视状态驱动"换图"，不再用矩形高亮

PolySpatial 2.4.3 支持 `VisionOSHoverEffect.EffectType.Shader`：系统把"是否被注视"作为强度值传给 Shader Graph（`PolySpatial Hover State` 节点），由 Shader 自己决定怎么画。工程里已经有示例 `Assets/Samples/PolySpatial/HoverComponent/ShaderGraphs/IntensityHover.shadergraph`。这样做有两个好处：真机上终于能看到设计好的 `_gaze` 图，而且显示完全按贴图的透明度来，不会再有矩形。

**① 新建 Shader Graph** `Assets/Resources/MoodiumUIRefresh/SpriteGazeSwap.shadergraph`，以及同目录的材质 `M_SpriteGazeSwap.mat`：
- URP Unlit，Surface = Transparent，Blend = Alpha，双面。
- 属性：`_MainTex`（Texture2D，引用名必须正好是 `_MainTex`，SpriteRenderer 才会填进去），`_GazeTex`（Texture2D）。
- 连线：`_MainTex` 和 `_GazeTex` 各在 UV0 采样；用 `PolySpatial Hover State` 的 `Intensity` 做 Lerp；结果再乘 `Vertex Color`（保留 `SpriteRenderer.color` 做的淡入淡出，比如 `SelectionSettingsVisual.SetAlpha`）。RGB 接 Base Color，A 接 Alpha。
- 前提：同一个按钮的 idle、gaze、pressed 三张图尺寸相同、都是 FullRect、不打图集，UV 才对得上。现有素材满足这一点（开场 498×282，设置 156²，旋钮 150²）。

**② `Assets/Scripts/Moodium/UI/UIRefreshVisuals.cs`**：加两个公用方法，`Button()` 改用它们。

```csharp
static Material s_GazeSwap;
public static void GazeHover(SpriteRenderer r, string gazeId)
{
    s_GazeSwap ??= Resources.Load<Material>("MoodiumUIRefresh/M_SpriteGazeSwap");
    r.material = new Material(s_GazeSwap);                 // 每个按钮一个材质实例
    SetGazeTexture(r, gazeId);
    var h = r.GetComponent<VisionOSHoverEffect>() ?? r.gameObject.AddComponent<VisionOSHoverEffect>();
    h.Type = VisionOSHoverEffect.EffectType.Shader;         // 不再画系统矩形
}
public static void SetGazeTexture(SpriteRenderer r, string id)
{ var s = Load(id); if (s != null && r.material.HasProperty("_GazeTex")) r.material.SetTexture("_GazeTex", s.texture); }
```

- `Button(sprite, hit, action, string gazeId = null)`：有 `gazeId` 就调用 `GazeHover`；没有就**不加任何注视效果**（设置面板的关闭按钮和文字选项没有注视图）。
- `Gear()`：`Button(r, ..., visual.Press, "settings_btn_gaze")`。
- `UIRefreshSpriteButton.Refresh()`：换完主图后，按状态调用 `SetGazeTexture(sr, m_Prefix + (m_Pressed ? "_pressed" : m_Open ? "_open" : "_gaze"))`，按下或面板打开时注视看到的就是对应的图。

**③ 开场按钮**：`OpeningVideoLanguageChoice.cs:73`、`OpeningVideoHardwareChoice.cs:75`

```csharp
// 旧
item.AddComponent<Unity.PolySpatial.VisionOSHoverEffect>().Type=Unity.PolySpatial.VisionOSHoverEffect.EffectType.Highlight;
// 新
Moodium.UI.UIRefreshVisuals.GazeHover(renderer, "MoodiumOpening/opening_btn_"+assetId+"_gaze");
```

`OpeningChoiceSpriteFeedback.Refresh` 换完图后，如果是按下状态，再调用 `SetGazeTexture(..., "MoodiumOpening/opening_btn_"+m_Id+"_pressed")`。编辑器里鼠标悬停换图的逻辑保留（编辑器里 Hover 强度一直是 0，只显示主图，互不影响）。

**④ 校准旋钮**：`CalibrationRotateDial.cs:20`

```csharp
// 旧
m_Knob.gameObject.AddComponent<VisionOSHoverEffect>().Type=VisionOSHoverEffect.EffectType.Highlight;
// 新
UIRefreshVisuals.GazeHover(m_Knob, "calib_rot_knob_gaze");
```

第 34 行拖动时换图的地方，同步调用 `SetGazeTexture(m_Knob, dragging ? (nature ? "calib_rot_knob_active_nature" : "calib_rot_knob_active_candy") : "calib_rot_knob_gaze")`。

**⑤ 手腕能量圆形按钮**（`CandyEnergyWristUI.cs:753`）：给圆形 `Image` 用同一个 Shader Graph 的材质实例，`_GazeTex` 用亮一档的圆形图；如果来不及，最少先删掉这里的 Highlight。

**⑥ 文字和方块类按钮**（`SensoryBagBranchUI.cs:36` 的 ‹ › 箭头、`WorldGuideCards.cs:324`、`MoodEntryChip.cs:41`、`GlassSlideToEnter.cs:411`、`SensoryBagCalibrator.cs:418`）：有注视图的照 ② 处理；没有注视图的，把 Highlight 挪到一个**形状和可见图形一致的不透明子网格**上（比如胶囊形网格），或者直接去掉。

### 2.3 不建议的做法

- **把精灵网格改成 Tight**：只能缓解一半。紧贴轮廓会把设计稿里的投影也算进去，高亮仍然比胶囊大一圈；而且 Highlight 在网格内部照样不看透明度。
- **调 Highlight 的颜色或强度**（`MoodEntryChip` 现在就这么做）：只是让矩形淡一点，并没有消失。
- **整个删掉注视效果**：底板没了，但真机上注视就完全没有反馈，对可用性不好。

### 2.4 验收

- 真机上逐个注视、捏合 2.1 表里的每个按钮：看得到 idle → gaze → pressed 的换图，四周没有任何矩形或半透明底板。
- 设置面板淡入淡出时，按钮跟着变透明（Vertex Color 生效）。
- 新增编辑器测试：上表里的按钮物体上都**没有** `EffectType.Highlight`；有注视图的按钮，材质里 `_GazeTex` 不为空。

---

## 3. 世界里的软模型：恢复原样（P0，先做这一步）

### 3.1 恢复两个 prefab

```bash
git checkout d9c4093e~1 -- Assets/prefab/CandySoft.prefab Assets/prefab/NatureSoft.prefab
```

- 恢复后，模型子节点重新引用原来的 `NatureSoft.fbx`（GUID `3033736efbf05408ca32f5766971444e`，带 −90° X 旋转、0.5 缩放）和原来的 `CandySoft.fbx`（GUID `b5f6924d252534a1ca530cdc10c860b1`）。根节点上的音效和组件本来就没变。
- **不要**再手工修改这两个 prefab 的模型子节点。

### 3.2 让包围盒计算不再被别的东西带偏（防御性修改）

以后软模型下面可能还会挂别的东西（比如第 4 节的 Moodi 不会挂上去，但粒子、贴花之类可能会），所以把下面三处"用全部 Renderer 算包围盒"的地方都改成**只算软模型本体的网格**：

| 文件 | 函数 | 改成 |
|---|---|---|
| `Assets/Scripts/Moodium/Calibration/CalibratedSensoryBagPresentation.cs` | `CalculateLocalRendererBounds` | 只统计 `MeshRenderer`（跳过 `SkinnedMeshRenderer`、`ParticleSystemRenderer`、`SpriteRenderer`、`LineRenderer`），并跳过挂在 `MoodiCompanionAnimator` / `MoodiVoice` 下面的物体 |
| `Assets/Scripts/Moodium/MoodiumRuntimeObjectSetup.cs` | `EnsureCollider` | 同上的过滤规则 |
| `Assets/Scripts/Interaction/ChocolateCapsuleInteraction.cs` | `CaptureBurstCenter` | 同上的过滤规则 |

把过滤写成一个公用的静态方法，例如 `SoftModelBounds.IsSoftBodyRenderer(Renderer r)`，三处共用，不要各写一份。

### 3.3 清理开洞方案留下的资产

在一个单独的提交里删除（之后需要可以整体 revert 回来）：

- `Assets/Model/Soft/`（`CandySoft_Mouth.fbx`、`NatureSoft_Mouth.fbx` 及 .meta）
- `Assets/Animations/Moodi/CandyMouthEmergence.controller`、`NatureMouthEmergence.controller`
- `Assets/Animations/Moodi/CandyMouthCollar.mat`、`NatureMouthCollar.mat`
- `Assets/Shaders/CandyMouthCollar.shadergraph`、`NatureMouthCollar.shadergraph`
- `Assets/Editor/MoodiEmergenceSetup.cs` 里生成洞口控制器和喉管材质的部分（保留"把 `Emerge` / `FallIn` / `Peek` / `Strain` 加进 `MoodiCompanion.controller`"的那部分，第 4 节要用）
- `Assets/Editor/MoodiEmergenceTests.cs` 里关于 `Mouth_Rig`、洞口骨骼、洞口动画的测试

**保留**：
- `Assets/Model/Moodi_Rigged.fbx`（v2：19 根骨骼，`Emerge` / `Strain` / `FallIn` / `Peek` 四段新动画）；
- `Assets/Animations/Moodi/OriginalClips/*.anim`（原 5 个动作，保持原时长）；
- `MoodiCompanion.controller` 里的 `Emerge` / `FallIn` / `Peek` 状态、`Strain` 叠加层，以及 `EmergeProgress` / `Press` / `Emerging` / `Peeking` 参数；
- `ISqueezeProgressDisplay`、`SqueezeProgressDisplayRouter`；
- `MoodiVoice.TryGetOutputLevel()` 和 `MoodiVoice` 里两处空组件判断的修复。

### 3.4 这一步的验收（先在编辑器里，再上真机）

1. `CalibratedSensoryBagPresentationTests` **全部通过**，包括上面那两项。
2. 新增测试 `SoftPrefabFitTests`：分别实例化 `CandySoft.prefab`、`NatureSoft.prefab`，用 40 × 25 cm 的默认校准和一组非默认尺寸（比如 30 × 18 cm）调用 `Configure`。断言 `VisualRoot` 下只算 `MeshRenderer` 的包围盒满足两点：X/Z 尺寸等于校准宽高（误差 < 1 mm）；X/Z 中心等于校准中心（误差 < 1 mm）。
3. 新增测试：软模型 `Configure` 之后，自动生成的 `BoxCollider` 的世界包围盒和软模型网格的世界包围盒在 X/Z 上重合（误差 < 2 mm）。
4. 真机：贴纸放好后，软模型以贴纸为中心出现；不调节时是 40 × 25 cm，调节框调多少就是多少；捏一下进度就涨（日志里能看到 `Chocolate Capsule Pinched`）。**这一步在真机上通过之前，不要开始第 4 节。**

---

## 4. 新的挤出方式：只用 Moodi 的骨骼动画，叠在软模型上升高（P1）

### 4.1 效果

- 软模型**完全不变**，不开洞。
- Moodi 站在原来透明视频的位置（软模型中心），身体的下半部分和软模型交叠：软模型不透明，所以藏在里面的部分自然被挡住，看起来就是 Moodi 被埋在软软的表面里。
- 随着进度，Moodi 用 `Emerge` 骨骼动画一点点往上冒：0% 只露小芽，20% 露出眼睛，50% 最宽处卡在表面，80% 两只鳍撑出来，100% 整个身子出来。按住时播 `Strain`（身体用力、往上挤一点），松手回弹。
- 100% 时：「啵」一下，喷出颗粒，Moodi 播 Happy 沿原来的弧线跳到上方，接着照原流程交给散步向导。

预览（Blender 渲染，原版软模型 + Moodi v2，4 cm 真实比例）：`Preview/rise_nature_stages.jpg`、`Preview/rise_candy_stages.jpg`。六格依次是 0%、20%、50%、50% 按住、80%、100%。`Preview/rise_wide_scale.jpg` 是整块软模型上的大小。

### 4.2 新组件 `MoodiRiseDisplay`（替换 `MoodiEmergenceDisplay`）

位置：`Assets/Scripts/Moodium/Reality/Emergence/MoodiRiseDisplay.cs`，实现 `ISqueezeProgressDisplay`。`SqueezeProgressDisplayRouter` 里的 `Mesh` 换成它。**`BindToSoft` 只要能找到校准过的软模型就用它，不再检查 `Mouth_Rig`**；资源缺失（`Moodi_Rigged` 控制器里找不到 `Emerge` 状态）时退回视频。

| 职责 | 做法 |
|---|---|
| 位置 | 和视频版一样以 `CalibratedSensoryBagPresentation` 为锚点：中心 = `presentation.CenterWorldPosition`，法线 = `presentation.AnchorOrientation * Vector3.up`。**高度不用视频版的 `softTopY`（整个软模型的最高点，中心处会悬空约 2.4 cm）**，改成"中心点周围 1.5 cm 半径内软模型网格顶点的最高点"。只在绑定时算一次，存成锚点的局部坐标，之后每帧跟着锚点走（和视频版的 `m_AnchorLocalPosition` 一样）。另外暴露参数 `m_SpotOffset`（Vector2，按软模型宽、深的比例），默认 (0, 0) |
| 朝向 | 上方向 = 软模型法线，正面朝向用户（`FlatTowardViewer`），**绑定时定一次，挤出过程中不转**。`Emerge` 的身体收紧是按正面烘焙的，转身会穿帮 |
| 大小 | 不改救援 Moodi 的大小：沿用 `MoodiRescueDirector.m_MoodiSize`（默认 4 cm）。另加参数 `m_RiseScale`（默认 1）：真机上嫌小时调成 1.3–1.5，跳出来的弧线途中平滑缩回 1 |
| Moodi 怎么对齐 | **不改挂父物体。** Moodi 仍然是 `MoodiRescueDirector` 的 `m_Root → m_Visual`。显示层只做两件事：① 每帧把 `m_Root` 放到 4.2 第一行算出的点，`m_Root.rotation` 设为上一行的朝向；② 把 `m_Visual.localPosition` 设成一个偏移，让 Moodi 骨架里名为 `Moodi_Rig` 的节点原点正好落在 `m_Root` 上。偏移在 Attach 时算一次：`offset = m_Visual.InverseTransformPoint(rig.position)` 取反后乘上 `m_Visual.localScale`。`Emerge` 动画的 0 高度就是 `Moodi_Rig` 原点所在的平面，所以只要原点贴着表面，0% 就只露小芽 |
| 进度 | `SetProgress(p)` 只记目标值；每帧 `SmoothDamp`（0.25 s，最快每秒 0.3），只涨不退（沿用 `MoodiEmergenceDisplay.Advance`）。Animator 参数 `EmergeProgress = ps`，`Emerging = true` |
| 按压 | `capsule.IsPinched` → `SqueezeProgressVideoController.StepSpring`（按住 2500 / 0.85，松手 220 / 0.32，每次按下冲量 2.5）。`Press = max(s, 0)` 给 `Strain` 层。**按住时不额外上移**（`Strain` 里已经有了）；只在 `s < 0`（回弹）时让 `m_Visual` 沿法线下沉 `0.07 × |s|` 个 Moodi 单位（Moodi 高 1.83 单位 = 4 cm，即 `0.07 / 1.83 × 4 cm × |s|`） |
| 「啵」 | `ps ≥ 0.995` 且目标 = 1：喷 14 颗颗粒（沿用 `MoodiEmergenceDisplay.Emit` 和 `Resources/MoodiEmergence/*Particles.mat`），调用一次 `CandyWorldManager.SpawnSoftSqueezeFeedback(中心)`（软模型上的白色圆形波纹，和掉进去时一样），然后触发 `Popped(中心)`。只触发一次 |
| 快眨 | 沿用 `MoodiEmergenceDisplay` 的规则：第 1、5、10、15 次按压；进度第一次到 20% 时连眨两下；跳出停稳后 0.3 s（`MoodiLandingBlink`）。闭 0.1 s、睁 0.1 s，闭眼缩放 0.08，在 `LateUpdate` 里改 `Eye_L` / `Eye_R` |
| 闷声晃小芽 | `SetPeeking(true)` 时 Animator `Peeking = true`；小芽摆幅读 `MoodiVoice.TryGetOutputLevel`，读不到就用 7 Hz 固定摆（沿用现有实现） |
| 泡泡世界 | `SetBubbleSuspended(true)` 时隐藏 Moodi 的 Renderer（沿用） |
| 糖果喷射避让 | `TryGetBurstExclusion` 返回 Moodi 所在点、半径 3.5 cm，让喷出的道具不要生在 Moodi 身上；`OverlapsBurstVideo` 用同一个圆判断 |
| 复位 | `ResetAndHide()`：参数清零，`m_Visual.localPosition` 还原，`Emerging` / `Peeking` = false，释放引用 |

### 4.3 和 `MoodiRescueDirector` 的衔接（逐段）

| # | 阶段 | 现在（视频版，`d9c4093e` 之前） | 改成 |
|---|---|---|---|
| 0 | 进入世界 | `BeginSqueezeRescue` → 等校准好的软模型 → `BindToSoft` | 不变。绑定时选显示层；软模型上什么都还没有 |
| 1 | 出场 | Moodi 从侧面滑到中心上方 30 cm，挥手 | 不变（目标点是 4.2 算出的表面点） |
| 2 | 滑倒、掉进去（K1） | 说 K1、播 Touched，0.4 s 落到中心；`Splash(1.6)`、白色波纹；Moodi 缩小到看不见 | 前半段不变。**接触那一刻**：`AttachMoodi`，用 0.12 s 把 `m_Visual.localPosition` 从 0 过渡到 4.2 的对齐偏移，同时 Animator 交叉淡入 `FallIn`（0.5 s：从露出 35% 沉到只剩小芽）；喷 6 颗颗粒，白色波纹照旧。**Moodi 不缩小、不隐藏** |
| 3 | 闷声 K2、K3 | Moodi 隐藏，声音闷闷的 | `SetPeeking(true)`：只露小芽，小芽随说话晃。闷声曲线不变。K3 说完 `FinishIntro`，再 `SetPeeking(false)` |
| 4 | 挤压（K4–K9） | 视频按进度播 | `Emerging = true`，每帧按 4.2 设参数。语音规则全部不变 |
| 5 | 「啵」 | 视频播完、罩子鼓起后消失，触发 `Popped` | 4.2 的「啵」。`FallbackRescue`：100% 后先等 1.2 s，如果显示层还在追进度，最多再等 3 s（不要 8 s：进度最快每秒 0.3，从 0.9 追到 1 只要 0.4 s 左右） |
| 6 | 跳出来（K10） | `JumpOut`：Moodi 从 0 缩放出现，沿弧线到上方 | Moodi 已经可见，不再从 0 缩放。`DetachMoodi`：`Emerging = false`，交叉淡入 Happy（0.1 s），用 0.3 s 把 `m_Visual.localPosition` 还原成 0（同时 `m_RiseScale` 缩回 1），然后沿原弧线飞到原位置，恢复 `MoodiCompanionAnimator`（`ResumeAfterRescue`） |
| 7 | 道谢、交接散步 | 说 K10，0.9 s 后 `Rescued(hover)` | 不变 |
| 8 | 退出 / 重进 | `StopExperience` → `ResetAndHide`、`StopAndClear` | 不变 |

**无故事模式**（没有配置 `guidePrefab`）：显示层自己生成 `Resources/MoodiEmergence/Moodi.prefab`，大小同样是 4 cm，从第 3 步的姿态开始（不飞入、不说话）。注意 `MoodiEmergenceDisplay` 现在是绑定后 8 秒才生成，改成绑定后立刻生成。

### 4.4 删掉 `MoodiEmergenceDisplay` 里这些做法

- `moodi.transform.SetParent(mouth, false)` 和后面的矩阵求逆、`lossyScale` 推算：不再改挂父物体。
- `LateUpdate` 里 `Vector3.forward * .01f * m_PressLift * spring`：重复上移，删掉。
- 所有 `Mouth_Rig`、`mouthAnimator`、`Mouth_*` 动画相关代码。

### 4.5 参数（全部暴露到 Inspector）

| 参数 | 默认 |
|---|---|
| 进度平滑时间 / 最快 | 0.25 s / 每秒 0.3 |
| 「啵」阈值 | 0.995 |
| 表面取样半径 | 1.5 cm |
| 位置偏移 `m_SpotOffset` | (0, 0)（按软模型宽、深的比例） |
| 挤出时 Moodi 放大 `m_RiseScale` | 1（真机上嫌小时调 1.3–1.5） |
| 回弹下沉 | 0.07 Moodi 单位 × \|s\| |
| 掉入对齐过渡 / 跳出还原 | 0.12 s / 0.3 s |
| 颗粒（掉入 / 啵） | 6 / 14 |
| 快眨 | 闭 0.1 s、睁 0.1 s，闭眼 0.08 |
| 100% 后最多再等 | 1.2 s + 3 s |

### 4.6 回退开关

在 `CandyWorldManager`（或 `SqueezeProgressDisplayRouter`）加一个序列化开关 `m_UseMoodiRise`，默认 `true`；为 `false` 时整个流程用原来的透明视频。用户说如果这版还不行就回退到视频版，到时只要关掉这个开关，不用回滚代码。

---

## 5. 测试与真机验收

### 5.1 编辑器测试

- 第 3.4 节的三组测试。
- `MoodiRiseDisplayTests`（替换 `MoodiEmergenceTests` 里还有意义的部分）：
  - 绑定后，`Moodi_Rig` 原点的世界坐标 = 表面点（误差 < 1 mm）；挤出期间 `m_Root` 不发生绕法线的旋转。
  - 进度只涨不退；`EmergeProgress` 从 0 到 1 不少于 3 秒。
  - 按住时 `Moodi_Rig` 原点相对表面的高度变化只来自 `Strain` 层（关掉 `Strain` 层时高度不变）。
  - 「啵」只触发一次；`ResetAndHide` 后 `m_Visual.localPosition == Vector3.zero`。
  - 找不到 `Emerge` 状态时，router 选视频。
- 跑 `MoodiRescue`、`SqueezeProgressVideoController`、`WalkTrailGuideController`、`MoodiAnimated`、`BubbleWorld`、`CalibratedSensoryBagPresentation`、`DualTrackingRealityFlow`、`PalmPressWorldSpaceContact` 这几组套件。**另外跑一次全套，把结果和 `initial-full-suite-failures.txt` 逐项对比：不能新增失败；已经存在的失败在验证文档里列出来，不能只写"相关套件全部通过"。**

### 5.2 真机验收（两个世界各一遍）

1. 开场：语言、感官袋两组按钮都能点，凝视和按下时没有半透明底板（第 1、2 节）。
2. 进世界：软模型以贴纸为中心；不调节时 40 × 25 cm，调节框调多少就是多少；倾斜贴纸时软模型跟着。
3. Moodi 从侧面出场、挥手、滑倒掉进软模型中心，只剩小芽；说 K2 / K3 时小芽晃。
4. 每捏一下进度条涨一格，Moodi 往上冒一点，按住时用力、松手回弹；20 / 50 / 80% 的礼物和语音照旧。
5. 100% 时「啵」，Moodi 跳出、道谢，散步向导接上；之后泡泡世界流程照旧。
6. 退出再进同一个世界、换另一个世界：没有残留的 Moodi，进度从 0 开始。
7. 选关轮播里的 Moodi（`Moodi meshy.prefab`）大小、朝向和以前一样，会呼吸浮动。
8. 把 `m_UseMoodiRise` 关掉，确认视频版完整可用。

---

## 6. 执行顺序

1. 第 3 节（恢复软模型 + 包围盒过滤 + 测试）→ 真机确认定位、尺寸、捏合进度都恢复。
2. 第 1 节（开场按钮）。
3. 第 2 节（按钮底色）。
4. 第 4 节（Moodi 挤出），带回退开关。
5. 第 3.3 节（清理开洞资产），单独提交。
6. 第 5 节全套验收，写 `docs/verification/2026-10-30-world-fix.md`：真机验收的每一条结果、全套测试和已有失败的对比、截图。
