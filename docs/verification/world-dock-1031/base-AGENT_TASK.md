# 世界悬浮按钮：替换手腕 UI + 修复一次点击按两下

基线：`8de6c1b`（`origin/codex/wish-ink-visibility-1031`）。在它上面新开分支，例如 `codex/world-dock-1031`。

如果预设心愿包（`Moodium-WishPresets-1031`）已经接入，两者改的文件没有重叠，可以叠加。

不要改点云、点大小、点材质，不要启用 GPU 运动着色器。

## 背景

用户测试发现，糖果 / 自然世界里的"从天而降"（能量满后的奖励雨开关）和"退出世界"两个按钮放在手腕上：
- 用户很难发现；
- 不熟悉 Vision Pro 的人也很难点到。

用户要求：
1. 改成直接悬浮在眼前的 UI；
2. 去掉手腕上的进度条；
3. 所有 UI 按钮都要同时支持看着捏合和手指直接点。

预览：
- `Preview/view_candy_zh.png`：第一人称视角
- `Preview/layout.png`：尺寸和跟随规则
- `Preview/states.png`：按钮各状态
- `Preview/demo_*.mp4`：完整流程演示

排查报告：`AUDIT_pinch_poke.md`。

---

## 包里的文件

| 包内路径 | 放到工程 |
|---|---|
| `Unity/Resources/MoodiumWorldDock/*.png`（43 张） | `Assets/Resources/MoodiumWorldDock/` |
| `Unity/Editor/WorldDockAssetPostprocessor.cs` | `Assets/Editor/` |
| `Unity/Scripts/WorldActionDock.cs`、`WorldDockButton.cs` | `Assets/Scripts/Moodium/` |
| `Unity/Editor/WorldActionDockTests.cs`、`InputDoublePressTests.cs` | `Assets/Editor/` |
| `Patch/world-dock-existing-files.patch` | 改 `CandyWorldManager.cs`、`WalkTrailGuideController.cs` |
| `Patch/input-double-press-fix.patch` | 改 `MoodiumSpatialButton.cs`、`SensoryBagCalibrationHandle.cs`、`SensoryBagDirectTouchInput.cs` |
| `Patch/after/*.cs.txt` | 打完补丁后的完整文件，只作对照 |
| `Source/` | 不进工程：贴图导出、预览渲染脚本 |

顺序：
1. 放 PNG 和导入脚本，对 `MoodiumWorldDock` 文件夹点一次 Reimport。
2. 放代码。
3. 打两个补丁：

```bash
git apply --3way /path/to/world-dock-1031/Patch/world-dock-existing-files.patch
```

```bash
git apply --3way /path/to/world-dock-1031/Patch/input-double-press-fix.patch
```

两个补丁我都确认过能在 8de6c1b 上干净应用，叠在一起也可以。

---

## A. 悬浮按钮 `WorldActionDock`

### 长什么样

两个液态玻璃胶囊并排，左边「从天而降」，右边「退出世界」。和开机按钮、心愿按钮是同一套玻璃做法。

- **大小**：每个 10 × 3.7 cm，中间留 2 cm。
- **贴图**：图标和文字烘进贴图，中英文各一套。
- **点击方式**：精灵图换图，不用自定义材质。

### 放在哪、怎么跟随（数值在 `WorldActionDock.cs` 顶部）

| 项 | 值 | 原因 |
|---|---|---|
| 位置 | 前方 50 cm、比眼睛低 21 cm（约 23°），面朝眼睛 | 在视野下方，不挡中央；在手臂可及范围内，能直接点 |
| 跟随 | 转头超出 35° 并保持 0.4 秒，0.6 秒滑到新的正前方 | 扫一眼旁边时不跟，真的转身才跟 |
| 移动 | 走动 30 cm，或高度变化 15 cm（坐下 / 站起）也跟 | |
| 低头看它 | 不跟随俯仰 | 低头去点时按钮不会跑开 |
| 手指附近 | 手指离按钮 12 cm 以内、或刚按完 1 秒内，**绝不移动** | 防止要点的时候目标滑走 |
| 防误触 | 刚出现、刚滑到位后 0.5 秒内不接受按压；两次按压至少间隔 0.4 秒 | |
| 隐藏 | 进入泡泡世界时隐藏（沿用 `WristHiddenFor`），回到房间后重新出现在正前方 | |

### 交互

每个按钮由三部分组成：
- 一个**胶囊形网格**，上面挂 `BoxCollider`（四周放大 6 mm，深 3 cm）、`MoodiumSpatialButton` 和 `VisionOSHoverEffect`；
- 玻璃贴图，是胶囊网格的**兄弟节点**，不是子节点；
- 一个承载缩放和抖动的父节点。

效果：
- **看着捏合、近处捏合、手指直接点**，都由现有的 `MoodiumSpatialInteractionController` 派发到同一个 `Press`。
- **注视高亮**落在胶囊形网格上，所以高亮是胶囊形，不会照亮整个矩形贴图（这是以前去掉精灵按钮高亮的原因）。贴图不挂在网格下面，所以高亮不会扩散到贴图上。
  - 真机上如果高亮效果不好，把 `m_SystemHover` 关掉即可。

### 「从天而降」三种状态

| 状态 | 条件 | 外观 | 按下 |
|---|---|---|---|
| 未解锁 | 能量 < 100% | 淡色玻璃 + 小字"能量集满后开启"，底部细线显示能量进度（世界主题色） | 按钮轻轻左右抖动，上方提示"继续捏一捏，集满能量就能用" 2.5 秒 |
| 可用 | 能量满、没在降落 | 世界主题色玻璃 | 触发 `ReleasePressed`，开始降落 |
| 降落中 | 奖励雨在下 | 主题色 + ■「停止降落」 | 触发 `ReleasePressed`，停止降落 |

- 能量满后，等 2.5 秒避开 Moodi 的感谢语，按钮上方提示一次"能量满啦，试试「从天而降」"（3.5 秒），按钮轻跳两下。每次进入世界只提示一次。
- 手腕上的能量进度条按要求去掉了。能量进度只剩这条细线，以及原有的模型能量环（`FloatingSqueezeProgress`）。
- 引导里"收集到的能量卡片飞向手腕"，改为飞进「从天而降」按钮（`ReleaseAnchor`）。

### 「退出世界」

- **默认点一次就退出**，和手腕 v3 时用户定下的一样。
- 提供开关 `m_ConfirmExit`（默认关）：打开后第一次点击变成"再点一次退出"（暖色，3 秒内再点才退出）。"再点一次退出"的贴图已经备好。如果真机上发现手扫过误退，再打开。

### 接入（补丁已包含）

- **`CandyWorldManager.cs`**：
  - `m_EnergyUI`（`CandyEnergyWristUI`）换成 `m_Dock`（`WorldActionDock`）。
  - 调用基本一对一：`Suppressed`、`SetReleaseAvailable`、`SetReleaseToggleState`、`SetExitAvailable`、`SetRoomInteractionEnabled`、`SetWorldTheme`、`SetProgress`，以及事件 `ReleasePressed`、`ExitPressed`。
  - dock **不挂在 manager 下面**：它在世界坐标里跟着用户，不能继承 manager 的缩放。`OnDestroy` 里一并销毁。
  - 进入世界时调用 `SetLanguage(...)`，语言跟随 `MoodiVoice.LanguageCode`，和心愿按钮一致。
- **`WalkTrailGuideController.cs`**：收集卡片的飞行目标从手腕改成 dock 的 `ReleaseAnchor`，条件从"手被追踪"改成"dock 可见"。

`CandyEnergyWristUI.cs` 及其测试**先不删**：
- 现在没有地方创建它了；
- 保留是为了不动现有的 4 个测试文件；
- 等真机确认新方案没问题后，可以另开一个提交删掉组件、`MoodiumWristUI` 资源和相关测试。

### 日志

```
[World Dock] glide to front (off 47°)
[World Dock] energy full, sky shower ready
[World Dock] sky shower start|stop
[World Dock] sky shower pressed while locked
[World Dock] exit
```

---

## B. 一次点击按两下（补丁 `input-double-press-fix.patch`）

详见 `AUDIT_pinch_poke.md`。我核实过代码：`SensoryBagDirectTouchInput` 在指尖离按钮还有 12 mm 时就按下，随后 visionOS 的触碰事件又经过中央派发器再按一次。

- `MoodiumSpatialButton.Press()`：0.3 秒内的重复按压忽略。新增常量 `PressCooldown`、方法 `IsRepeat`。
- `SensoryBagCalibrationHandle.PointerStarted`：确认、重置、升高、降低这几个一次性按钮同样 0.3 秒防连按。拖动手柄不受影响。
- `SensoryBagDirectTouchInput`：加 `[DefaultExecutionOrder(-50)]`，先于派发器执行。系统触碰一开始，它先放开自己抢的指针，派发器的 `PointerStarted` 就不会被当成第二个指针拒收，滑块和拖动不再弹回。

---

## 测试

- **`WorldActionDockTests.cs`（13 项）**：
  - 位置在视野下方 20–32° 且伸手可及；低头 / 抬头不改变位置；面朝眼睛；
  - 跟随条件（转头角度、持续时间、走动、坐下）；角度按水平面算；手指附近不移动；
  - 按压时机；两个按钮可点区域不重叠且足够大；
  - 状态和贴图名；38 张按钮贴图、提示条、九宫格细线都能加载且尺寸正确；
  - manager 接入正确、不再创建手腕 UI；泡泡世界里隐藏；
  - `Build` 生成两个带碰撞体的按钮，贴图不在碰撞体下面。
- **`InputDoublePressTests.cs`（4 项）**：防连按、执行顺序、校准按钮防连按。

请再补以下测试：
1. 用 EditMode 驱动 `LateUpdate`（反射调用，或抽出可注入时间的方法）：转头 50° 后，0.4 秒内不动、1 秒后到位；手指在 10 cm 内时不动。
2. 未解锁时按「从天而降」不触发 `ReleasePressed`；可用时触发。
3. `m_ConfirmExit = true` 时，第一次不退出，3 秒内第二次退出，超过 3 秒重新计。

跑全量 EditMode，和 wish-ink-visibility-1031 的基线对比（730 项，29 项已有失败），不应新增失败。

> 我这边没有 Unity，也没有 C# 编译器，代码**没有编译过**。编译如果报错，请直接修，并在验证记录里说明。

---

## 真机验收

1. **能不能发现**：
   - 让没用过的人进入糖果、自然世界，不提示，看他们多久注意到下方的按钮。
   - 太靠下没人看到，就把 `Drop` 从 0.21 调到 0.18；挡视线就调到 0.24。
2. **两种点法**：
   - 两个按钮分别用"看着捏合"和"伸手直接点"各试 10 次，每次只触发一次。
   - 左手、右手都试。
3. **注视高亮**：
   - 看着按钮时，高亮是胶囊形，没有矩形底板。
   - 如果不好看，把 `m_SystemHover` 关掉。
4. **跟随**：
   - 左右扫视（不超过 35°）时按钮不动；转身后按钮滑到前面。
   - 伸手去点时按钮不会跑；坐下 / 站起后位置跟着调整。
5. **能量**：
   - 细线随能量增长；满后提示一次、按钮跳两下。
   - 点击开始降落，再点停止。
   - 未满时点击会抖动并显示提示。
6. **退出**：点一次退出。如果测试中出现误退，打开 `m_ConfirmExit` 再测。
7. **泡泡世界**：进入时按钮消失，回到房间后出现在正前方，不在背后。
8. **一次点击不按两下**：
   - 校准时点「升高」一次只升 5 mm。
   - 无感官袋轮播的 ‹ › 每点一次只翻一页。
   - 玻璃滑块用手指拖动不弹回。

## 交付

在 `docs/verification/` 写一份记录，内容包括：
- 测试数量和结果；
- 编辑器里两个世界的悬浮按钮截图（未解锁、可用、降落中）；
- 编译时修过的问题。

先不要推送，等用户确认。
