# 教学卡大字、心情面板文字黑底、移动条只移动视频（2026-10-30）

基准：`codex/visual-revert-1030` 的 `6be2e21`。

**这份改动在一台没有 Unity 的机器上写成，没有编译，也没有跑测试。**

## 1. 进度 100% 后，"?" 打开的教学卡上有一段字特别大

### 原因

- 100% 之后，"?" 打开的教学卡第一页是"自由探索"页（`WorldGuideCards`，`includeExplore`）。
- 这一页的整张卡是预先画好的图片，文字都在图里。只有一行"找到地上的光环站进去，或把手伸进袋子旁的小光环，会有惊喜"是代码另外加的 3D 文字（`UpdateRingCaption`，`0fbac1f` 加入）。
- 它的字号写成了 `fontSize = 1.4`。TextMeshPro 3D 文字是每 1 号字 0.1 m，所以字身约 14 cm，大约是卡片上其他小字的 10 倍，折成好几行盖住了大半张卡。

### 改法：整行删除

用户确认：**现在体验里已经没有"光环"这个环节了，这行字不需要。** 所以不调字号，直接删掉。

- `WorldGuideCards.cs`：删除 `m_RingCaption`、`RingCaptionFont`、`RingCaption()`、`UpdateRingCaption()`，以及 `ApplyPage` 和 `SetAlpha` 里对它的调用。"自由探索"页只剩预先画好的卡片图和按钮。
- `CandyWorldManager.cs`：删除给它传字体的那一行（`m_WorldGuide.RingCaptionFont = …`）。

## 2. 心情面板的文字背后有时出现黑色方块

### 原因

- visionOS（RealityKit）**不认 Unity 的 `sortingOrder` / `renderQueue`**。透明物体的前后顺序只有两种来源：
  - 有 `VisionOSSortingGroup` 时，按组里写的顺序画；
  - 没有时，RealityKit 把每个物体单独按距离排序。
- 心情面板（`Resources/Moodium/MoodCheck/MoodCheckPanel.prefab`）**没有任何 `VisionOSSortingGroup`**。它由三十多个文字、二十多层玻璃和贴图组成，全靠 RealityKit 自己排。
  - 面板单独放着时，距离差异稳定，顺序大多是对的，所以"有时候正常"。
  - 一旦旁边有别的大块透明物体（透明底视频、选关的 3D 世界模型）离得很近或穿插，排序会翻转：文字先画，后面的玻璃后画。
  - 每个字其实是一个方形面片。最可能的机制是：方片透明的部分也参与了深度，后画的玻璃在这些方形里被挡掉，露出来的就是字背后一块块深色方块。具体机制没有在真机上逐帧确认，但"缺排序组 → 只在靠近别的透明物体时出错"这一点和现象完全吻合。
- 项目里其他没有这个问题的组件早就这样处理过。`GlassSlideToEnter` 的注释原话："RealityKit needs a native sorting group: Unity sortingOrder/renderQueue alone does not establish a stable order for overlapping transparent UI." `MoodRecommendationView` 也一样。只有心情面板漏了。
- 2026-10-29 交接文档里的"心情表盘文字黑底严重"就是这个问题。当时只清了描边和阴影，没有解决排序。

### 改法（`MoodCheckController.cs`）

- 面板生成后，加一个 `VisionOSSortingGroup`，`DepthPass = PostPass`，和推荐卡、滑动进入按钮一致。
- 每个 Renderer 都登记进去，顺序直接用 prefab 里已经设好的 `sortingOrder`：玻璃 0–2，表盘 10–23，文字 30。
- 中文常用到的 TMP 备用字体子网格（`TMP_SubMesh`）跟随它所属文字的顺序。
- 推荐卡有自己的排序组，所以把它的子物体排除在外。
- 每帧检查 Renderer 数量：新出现备用字体子网格、推荐卡生成新图层时重新登记。只在数量变化时才重建。

## 3. 开机界面的移动条只移动视频，不移动心情面板和设置面板

### 现在的结构

有感官袋时，选关阶段（视频背景）的情况：

| 物体 | 怎么放 | 跟不跟移动条 |
|---|---|---|
| 透明底视频 | 视线前方 1.35 m，高 1.15 m | 移动条就是拖它 |
| 心情按钮、设置齿轮、"请对准贴纸" | 视频的子物体，在视频平面前 4 cm | 跟 |
| **心情面板**（点心情按钮打开） | 打开那一刻，在**头部**前方 0.75 m、眼睛下 12 cm | **不跟** |
| **设置面板**（点齿轮打开） | 打开那一刻齿轮的位置往上，离视频平面只有 4 cm | **不跟** |

- 移动条（`MoodiumWindowHandle`）只移动视频的根物体。两个面板是独立放在世界里的，位置只在打开那一刻算一次，之后和视频没有任何关系。
- 所以在面板打开时拖视频，或者先把视频拉近、转过头再打开面板，面板就会和视频穿插。
- 穿插时，第 2 条的排序问题就会出现。设置面板本身离视频只有 4 cm，本来就在临界距离。

没有感官袋时，选关是 3D 转盘，视频是隐藏的，没有移动条。打开心情面板或设置时转盘会整个隐藏，所以这条路不受影响。

### 应该遵循的原则

**视频是"主窗口"，从它打开的面板是它的"子窗口"：**
1. 面板永远站在视频前面，离视频平面一个固定距离（0.3 m），中间不穿插。
2. 拖移动条时，视频和打开着的面板作为一个整体移动、转向，相对位置不变。
3. 整体不能被拉到离眼睛太近（视频至少 0.85 m，所以面板至少离眼睛 0.55 m）。

不直接把面板挂成视频的子物体，是因为视频根物体是不等比缩放的（1.28 × 0.72），挂上去面板会被压扁。

### 改法

| 文件 | 改动 |
|---|---|
| `MoodiumWindowHandle.cs` | 新增"跟随物体"：`AddFollower` / `RemoveFollower`。拖动开始时记下每个跟随物体相对视频的位置和朝向（不含缩放），拖动中同步。销毁的跟随物体自动移除。新增 `MinDistanceFromViewer`：拖动时视频离视线水平距离不小于这个值 |
| `MoodiumAppFlowController.cs` | 有感官袋的选关里：心情面板改为放在视频正前方 0.3 m（高度仍是眼睛下 12 cm），并登记为跟随物体；设置面板在齿轮位置上再往前 6 cm（离视频 10 cm），也登记为跟随物体；开机视频的移动条最小距离 0.85 m。常量 `OpeningPanelClearance`、`OpeningWindowMinDistance`、`SettingsPanelExtraClearance` |

## 4. 测试

| 测试 | 内容 |
|---|---|
| `WorldGuideLifecycleTests.ExplorePageIsTheCardOnlyWithoutTheOldRingHint` | 打开带"自由探索"页的教学卡（中、英），卡片上没有任何 TMP 文字 |
| `MoodCheckControllerTests.PanelHasANativeSortingGroupWithTextAboveGlass` | 面板有 PostPass 排序组；文字顺序高于玻璃和表盘；每个可见文字都登记了；备用字体子网格和文字同序 |
| `MoodiumWindowHandleTests`（新文件） | 面板在视频前 0.3 m、朝向一致；拖动并转向后面板相对视频的位置朝向不变（视频不等比缩放）；视频不能被拉到 0.85 m 以内；销毁的跟随物体被移除 |
