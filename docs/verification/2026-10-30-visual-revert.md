# 视觉回退与朝向修复（2026-10-30 下午）

基于 `feb0a28`（world-fix-1030 全部步骤之后）。用户真机反馈：
- 设置按钮、开场按钮变成纯黑底，很难看；
- 挤出的 Moodi 侧身 90°、太大；
- 选关吉祥物背对用户；
- 无感官袋时选关处看不到设置按钮；
- 其他 UI 有黑底、位置不对；
- 泡泡世界点云变成锋利的方块。

**这台机器没有 Unity，以下改动没有编译，也没有跑测试。** 第一件事是在 Unity 里打开、编译、跑下面列出的测试，再上真机。

## 1. 按钮：去掉注视 Shader，开场按钮回到第一版液态玻璃

- 黑底的原因：`de50a94` 新增的 `SpriteGazeSwap` Shader Graph（PolySpatial Hover State 切换 idle / gaze 贴图）在真机上渲染成黑色。已经整套删除：
  - `UIRefreshVisuals.GazeHover`、`SetGazeTexture`
  - `GazeMaterialOwner`
  - `Resources/MoodiumUIRefresh/SpriteGazeSwap.shadergraph`、`M_SpriteGazeSwap.mat`
  - `BubbleSplatGraphBuilder.BuildGazeSwap`
- 精灵按钮（设置齿轮、设置面板、校准旋钮）不再挂任何 `VisionOSHoverEffect`：
  - Highlight 会点亮整张矩形贴图，就是之前那块半透明底板；
  - Shader 方式变黑。
  
  现在只画按钮图本身，按下时仍然换成 `_pressed` 图。
- 开场的语言、感官袋按钮恢复成 `f8ab1d9`（第一版液态玻璃）的做法：画布上的 UGUI `Image`，没有注视效果，没有自定义材质，`Button.transition = None`，按下时换 `_pressed` 图。
- 和 `f8ab1d9` 的两处不同，都是为了修那一版的问题：
  1. **尺寸**：按钮区域从 330×114 改成 330×187。原图是 498×282，`f8ab1d9` 硬塞进 330×114 会把胶囊和字压扁；`3f42e30` 修比例时又整体缩到 0.9，这次不缩。
  2. **碰撞盒**：改成 240×96，只盖住可见的胶囊（219×76），不盖透明投影。
- 保留了之后的有效修复：
  - 选中后 0.3 秒再切视频；
  - 选中后不能重复选；
  - 问题文字不挡射线；
  - 开场捏合完成后关掉视频碰撞盒。
- 位置：两个按钮回到 `f8ab1d9` 的 (±165, −160) 画布坐标。胶囊之间约 110 画布单位，画布在视频碰撞盒前面。

## 2. 挤出 Moodi：朝向和大小

**朝向**
- 原因：`MoodiRiseDisplay.AttachMoodi` 把救援根节点转到 `LookRotation(-toward)`，但 `MoodiCompanionAnimator` 飞入时已经把动画模型转过一次来面向用户（它转的是模型，不是根节点），模型的正面也不是根节点的 +Z。两个旋转叠在一起，偏转角度取决于用户站的位置，实测侧身 90°。
- 修复：挂上去后，用两眼中点相对 `Body_Upper` 的方向算出真实的正面（同 `MoodiCompanionAnimator.FindFaceDirection`），再绕软模型法线转到正对用户，然后把这个朝向存进锚点。

**大小**
- 原因：大小原本按 Renderer 包围盒的最长边定成 4 cm。v2 的蒙皮网格在第一次蒙皮之前包围盒不可靠。
- 修复：改为按眼睛骨骼的间距测量（静止姿态下眼距 0.678、最长边 1.904 个骨架单位），救援 Moodi（`MoodiRescueDirector.CreateMoodi`）和挤出时（`MoodiRiseDisplay.m_RiseSize`，默认 4 cm）都用这个方法。日志 `[Moodi Rescue] size from bounds … from eyes …` 会打出两种方法的结果对比。真机上如果仍然嫌大，只需要改 `m_RiseSize`。

**测试**：新增 `WorldFix1030Tests.RiseFacesTheViewerAndKeepsFourCentimetres`（三个观察方向，正面误差 < 2°，大小 4 cm ± 4 mm）。

## 3. 泡泡世界点云：回到圆形柔边点

- 原因：
  - 之前的圆点靠的是 URP Unlit 加 Alpha Clip 阈值 0，这个组合在 visionOS 上会用 alpha-to-coverage，把每个带 4×4 圆斑贴图的小方片显示成柔边圆点。
  - `d9c4093` 改成 `BubbleSplatMotion` Shader Graph，它没有 alpha-to-coverage，4×4 圆斑本身又是方的，于是每个点都成了锋利的方块。
  - 验证截图里能看到方片，但比较基准用的是编辑器透明材质，所以没发现。
- 修复：`BubbleSplatMaterial.Create` 默认回到之前用户认可的 URP Unlit 圆点材质（`0fbac1f`–`26bb73d` 的设置）。运动 Shader 只在显式传 `motion: true` 时使用，编辑器预览、验证工具和相关测试已经改成传 `true`。
- 代价：这一版点云是静止的（`d9c4093` 已删掉原来的 `BubbleWorldAmbientWind`、`BubbleWorldHandRipple`）。
- 要恢复"圆点 + 摆动"，在 `BubbleSplatGraphBuilder.Build()` 里用 UV0 在 Shader 内算圆形遮罩（UV0 → 取 4×4 块内坐标 → 归一化到 −1…1 → 1 − 距离²，平方后接 Alpha，按 seed 给 1–1.6 的椭圆比例），并在材质上设 `_AtlasSize`，重新生成 Shader Graph，在**真机**上确认是圆点后，再把 `BubbleWorldController` 的调用改成 `motion: true`。

## 4. 选关吉祥物朝向

- 原因：`d9c4093` 重跑 `MoodiAnimatedSetup.UpgradePrefab` 时，重新计算了 `Moodi meshy.prefab` 里 `moodi_rigged` 实例（`Moodi_Rigged.fbx`）的变换，结果是：
  - 绕 Y 轴转了 180°；
  - 缩放从 0.1 变成 0.139；
  - 抬高了 6.4 cm。
  
  新旧 FBX 在静止姿态下完全一致，所以朝向反了完全是这个实例变换造成的。`CandyWorldUI` / `NatureWorldUI`（贴纸选关、无感官袋轮播）都嵌套了这个 prefab，所以选关吉祥物背对用户。
- 修复：
  - `Moodi meshy.prefab` 和 `Resources/MoodiEmergence/Moodi.prefab` 的这个实例恢复成 `d9c4093` 之前的值：不旋转、缩放 0.100004、原点。`m_BodyCenterLocal`（手碰和转身的中心）因此重新对得上。
  - `UpgradePrefab` 重跑时保留已有实例的变换，只有第一次升级才重新计算。
- 测试：`WorldFix1030Tests.MoodiModelInstanceIsNotTurnedOrRescaled`。

## 5. 无感官袋轮播里的设置按钮

- 原因：齿轮放在轮播根节点的 (0, −0.39, −0.08)。轮播在眼前 0.75 m、眼睛以下 0.12 m，所以齿轮在视线以下约 37°，看不到也点不到。贴纸模式下齿轮只在视线以下约 12°，所以那边能看到。
- 修复：
  - 心情入口那一行从 y = −0.30 提到 −0.27；
  - 齿轮放到它右边 (0.215, −0.27, −0.08)，心情入口占 x ±0.16，两者不重叠；
  - 轮播整体从眼睛以下 0.12 m 改成 0.08 m。
  
  设置面板在齿轮上方 0.138 m 打开，会跟着上移。

## 6. 其他 UI

- **轮播里的 Moodi 不再改成透明材质**：轮播会把模型的所有材质复制一份改成透明（不写深度），用来淡化两侧的世界，Moodi 的蒙皮网格也在其中，RealityKit 下会看到眼睛、身体内侧透出来。现在跳过 Moodi，世界模型照旧淡化。
- **设置面板里"打开状态"的齿轮**从和面板同一个平面移到面板前 2 mm，避免闪烁。
- **没有改的**：
  - 能量环位置。理论上软模型下的粒子会撑大包围盒，但没有证据，没动。
  - 贴纸模式下齿轮压在视频画面下部。能用，暂时没动。

## 需要跑的测试

`IntroVideoGateTests`、`UIRefreshTests`、`WorldFix1030Tests`、`MoodiAnimatedTests`、`MoodiBreathingFloatTests`、`StickerWorldSelectionControllerTests`、`RealityWorldModelCarouselTests`、`BubbleSplatMaterialTests`、`BubbleSplatMotionTests`、`MoodiRescue*`、`SensoryBagBranchTests`，然后全套和 `world-fix-1030/remaining-failures.md` 对比，不能新增失败。
