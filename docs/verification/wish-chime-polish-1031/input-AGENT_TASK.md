# 心愿风铃打磨：液态玻璃按钮 + 跟随泡泡防误触

基线分支：`origin/codex/bubble-play-1031` @ `1af429c`（已包含心愿风铃）。在它上面新开分支，例如 `codex/wish-chime-polish-1031`。
不要改点云、点大小、点材质，不要启用 GPU 运动着色器。

本包两件事：

- **A. 重写 / 放飞按钮换成液态玻璃风格**：和开机按钮同一套做法（预渲染贴图，不用自定义材质）。
- **B. 跟随泡泡不再和风铃抢位置**：下一层入口泡泡（`m_Nested` 的 Portal）和退出泡泡（`m_Home`）一直跟着视线，常常和空白风铃、写字纸条重叠。现在泡泡只要指尖碰到就触发，写到一半很容易误进下一层或误退出。

示意图：`Preview/bubble_yield_layout.png`、`Preview/buttons_in_worlds.png`。

---

## 包里的文件

| 包内路径 | 放到工程 |
|---|---|
| `Unity/Resources/MoodiumBubblePlay/WishButtons/*.png`（22 张） | `Assets/Resources/MoodiumBubblePlay/WishButtons/` |
| `Unity/Editor/WishButtonAssetPostprocessor.cs` | `Assets/Editor/` |
| `Unity/Scripts/BubbleWorld/Play/WishActionButton.cs` | `Assets/Scripts/Moodium/BubbleWorld/Play/` |
| `Unity/Scripts/BubbleWorld/Play/ChimeFocus.cs` | `Assets/Scripts/Moodium/BubbleWorld/Play/` |
| `Unity/Editor/WishChimePolishTests.cs` | `Assets/Editor/` |
| `Source/tools/export_wish_buttons.py` | 不进工程。重新导出贴图用（需要 Chrome、ffmpeg） |
| `Source/tools/make_preview.py`、`layout_diagram.html`、`preview-bg/` | 不进工程。生成预览图用 |

先放 PNG 和导入脚本，对 `WishButtons` 文件夹点一次 Reimport，再放代码。`.meta` 由 Unity 生成，记得一起提交。

---

## A. 液态玻璃按钮

### 贴图

`wish_btn_{内容}_{状态}.png`，文件名就是接口。

| 内容 | 文字 | 状态 |
|---|---|---|
| `rewrite_zh` / `rewrite_en` | ↺ 重写 / Rewrite（中性白玻璃） | idle、gaze、pressed |
| `release_candy_zh` / `release_candy_en` | ↑ 放飞 / Release（底部透一点粉） | idle、gaze、pressed、disabled |
| `release_nature_zh` / `release_nature_en` | ↑ 放飞 / Release（底部透一点嫩绿） | idle、gaze、pressed、disabled |

- 配方和开机按钮一致：白色渐变底、1.5 px 边缘高光、顶部反光、柔和阴影。文字已经烘进贴图。
- 和开机按钮不同的地方：
  - 糖果第一层和雪原背景很亮，所以底下多了一层很淡的深紫烟雾（24%），文字阴影也加重，保证白字看得清。
  - 放飞是主操作，带世界主题色。
- 尺寸：3 倍导出，每张 996 × 564 px。导入后 PPU = 7333.33，胶囊本体在场景里是 **0.090 × 0.031 m**，整张贴图（含阴影边）0.136 × 0.077 m。不需要再缩放物体。

### 改 `WishChime.cs`

1. **删掉**现在的 `Button(string,string,float,Action)`：圆角平板网格、纯色材质、TMP 文字都不要了。`ownedMeshes` 里的平板网格也随之不再产生。

2. 在 `BeginWrite()` 里，创建 `buttons` 之后改成：
   ```csharp
   bool en = Moodium.Characters.MoodiVoice.LanguageCode == "en";
   rewriteButton = AddAction("Rewrite", WishActionButton.Prefix(false, candy, en), -WishActionButton.Offset, Rewrite);
   releaseButton = AddAction("Release", WishActionButton.Prefix(true, candy, en), WishActionButton.Offset, ReleaseWish);
   ```
   ```csharp
   WishActionButton rewriteButton, releaseButton;
   float lastStrokeEnd = -10f;
   int lastWriter = -1;

   WishActionButton AddAction(string name, string prefix, float offset, Action action)
   {
       var holder = new GameObject(name).transform;
       holder.SetParent(buttons.transform, false);
       holder.position = Strip.TransformPoint(frame.Surface(Vector2.one * .5f)) + viewer.transform.right * offset;
       holder.rotation = Quaternion.LookRotation(viewer.transform.forward, Vector3.up);
       // Parents may be scaled with the world; keep the sprite at its imported metric size.
       Moodium.UI.UIRefreshVisuals.UnitScale(holder);
       return WishActionButton.Create(holder, name + " Glass", prefix, action, PressBlocked, 5);
   }

   // A press that lands while a stroke is being drawn, or right after, is the writing hand brushing past.
   bool PressBlocked() => current != null || Time.time - lastStrokeEnd < .35f || clearing || saving;
   ```
   - 两个按钮中心离纸条中心左右各 **12 cm**（原来 10 cm），和放大后的纸条之间留 3.25 cm 空隙。
   - 高度和纸条中心齐平。
   - sorting order 用 5，比风铃本体的 4 高一级。

3. `FinishStroke()` 末尾记录 `lastStrokeEnd = Time.time;`。开始新笔划时（`current == null` 且要新建）记录 `lastWriter = hand;`，并公开出去：`public int LastWriter => lastWriter;`。B 部分要用它判断哪只手在写。

4. `Update()` 的 Writing 分支里，每帧同步一次可用状态（`SetInteractable` 内部只在变化时换贴图）：
   ```csharp
   if (releaseButton != null) releaseButton.SetInteractable(HasInk);
   if (rewriteButton != null) rewriteButton.SetInteractable(HasInk || current != null);
   ```
   没写字时放飞显示 `disabled` 贴图，按了也没反应。重写没有 disabled 贴图，组件会把 idle 贴图调到 45% 透明。

5. 编辑器里鼠标悬停时，如果现有输入能拿到 hover，调 `SetHovered(true/false)`；拿不到就不用管。真机上 RealityKit 不报注视。**不要**加 `VisionOSHoverEffect`：系统高光会把整个矩形贴图连透明边一起点亮，原因见 `UIRefreshVisuals` 里的注释。

6. 放飞后 `buttons.SetActive(false)` 保持不变。按钮按下时贴图换成 pressed，0.15 秒后恢复，组件已经处理。

### 不要做

- 不要给按钮加自定义 shader 或材质，用 `SpriteRenderer` 的默认 sprite 材质，和开机按钮、设置齿轮一样。
- 不要把文字改回 TMP 叠在贴图上，文字已经烘进贴图了。

---

## B. 跟随泡泡给风铃让位

### 现在的问题（在代码里）

- 空白风铃生成在视线前方 0.55 m、下方 0.25 m（`WishChimeGarden.Create(null)`）。
- 跟随泡泡在前方 0.6 m、下方 0.25 m（`PortalPosition()`，以及 `m_Home` 的同样算法）。两者几乎重合。
- 泡泡的触发方式：
  - **指尖距离**：`FingertipTouching`，不需要捏。
  - **注视 + 捏合**：`m_Home` 挂着 `MoodiumSpatialButton`。写字本身就是捏合，看着泡泡写字会直接退出。
- 泡泡视线偏离 60° 就重新定位，所以低头看按钮、转头时它会滑到手边。
- 现在 `BubbleWorldPlay` 的 `exclusion` 只防涟漪，不防泡泡本身。

### 规则（数值都在 `ChimeFocus.cs` 里）

1. **禁区，一直生效**：只要有空白风铃或正在写字的风铃，跟随泡泡的目标点就不能进入风铃周围 **35 cm**（水平距离，加上泡泡自己的半径）。
   - 进了就用 `ChimeFocus.AvoidKeepOut()` 绕着人转到侧边，距离和高度不变。
   - 平时跟随的其他行为不变。

2. **风铃专注**：满足任一条件就进入。
   - 正在写字（`garden.Writing != null`）；
   - 有风铃正在飞往挂点（`State == Flying`）；
   - 任一只手离空白风铃不到 **25 cm**（用 `WishChime.Near(h.Tip, .25f)`）。

   条件消失后再保持 **1.2 秒**（`ChimeFocus.NextFocusUntil` / `Active`）。

3. **专注时：让位 + 固定 + 上锁**
   - **让位**：泡泡在 0.6 秒内滑到 `ChimeFocus.ParkPosition(eye, gazeYaw, writerHand)`，即不写字那只手那侧 50°、距离 0.6 m、比眼睛低 0.15 m。
     - 右手写字（`writerHand == 1`）就停左边；左手就停右边；不知道是哪只手就当右手。
     - 同时缩到 **70%**、透明度降到 **55%**。
   - **固定**：专注期间泡泡不再跟着视线走。只有停靠点离开视线超过 **80°**（人真的转身了）才重新算停靠点。
   - **上锁**：
     - 指尖碰一下不触发；
     - `m_Home` 的 collider 关掉，注视加捏合也不触发；
     - 编辑器鼠标点击也不触发。
   - **真想离开**：手指在泡泡里按住 **0.6 秒**（`BubbleHoldGate`）。泡泡前面出现一圈细光环，按进度填满；满了才进入下一层或离开；中途手拿开，进度清零。

4. **解除专注**：
   - 泡泡在 0.8 秒内滑回正常跟随位置，同时恢复大小和透明度。
   - 把 `m_PortalArmed` / `m_HomeArmed` 设回 `false`，指尖先离开泡泡一次，触碰才重新生效。另外解除后 **0.8 秒内**不接受触碰（`UnlockGraceSeconds`）。

### 改动位置

**`WishChimeGarden.cs`**
```csharp
public bool FocusActive { get; private set; }
public int WriterHand { get; private set; } = -1;   // 0 left, 1 right, -1 unknown
float focusUntil;

// Update() 末尾
bool flying = false; foreach (var c in chimes) if (c != null && c.State == WishChime.Stage.Flying) { flying = true; break; }
float nearest = float.PositiveInfinity; int nearHand = -1;
if (blank != null && blank.State == WishChime.Stage.Blank)
    for (int i = 0; i < 2; i++) { var h = Hands.Hands[i]; if (h.Valid && blank.Near(h.Tip, ChimeFocus.HandNearChime)) { nearest = 0f; nearHand = i; } }
if (Writing != null && Writing.LastWriter >= 0) WriterHand = Writing.LastWriter; else if (nearHand >= 0) WriterHand = nearHand;
bool raw = ChimeFocus.RawFocus(Writing != null, flying, blank != null, nearest);
focusUntil = ChimeFocus.NextFocusUntil(raw, Time.time, focusUntil);
FocusActive = ChimeFocus.Active(raw, Time.time, focusUntil);

/// Centre of the space a chime needs: the writing strip while writing, otherwise the blank chime.
public bool TryKeepOut(out Vector3 centre)
{
    if (Writing != null && Writing.Strip != null) { centre = Writing.StripCentre; return true; }
    if (blank != null && blank.State == WishChime.Stage.Blank) { centre = blank.transform.position; return true; }
    centre = default; return false;
}
```
`WishChime` 里加 `public Vector3 StripCentre => Strip.TransformPoint(frame.Surface(Vector2.one * .5f));`。`Leave()` 里把 `FocusActive = false; WriterHand = -1;` 复位。

**`BubbleWorldPlay.cs`**：转发给控制器。
```csharp
public bool ChimeFocus => garden != null && garden.FocusActive;
public int WriterHand => garden != null ? garden.WriterHand : -1;
public bool TryChimeKeepOut(out Vector3 centre) { centre = default; return garden != null && garden.TryKeepOut(out centre); }
```
注意：`SetInside(false)` 后 `gameObject` 不激活，`garden.Update` 不跑，必须保证此时 `FocusActive` 是 false（`Leave()` 已复位）。

**`BubbleWorldController.cs`**：`Update()` 的 Inside 分支，对入口泡泡和退出泡泡用同一套逻辑。
```csharp
bool focus = m_Play != null && m_Play.ChimeFocus;
var eye = viewer.transform.position; var yaw = GazeYaw(viewer.transform.forward);
if (focus)
{
    if (!m_Parked || Vector3.Angle(viewer.transform.forward, m_ParkTarget - eye) > ChimeFocus.ReparkAngle)
        m_ParkTarget = ChimeFocus.ParkPosition(eye, yaw, m_Play.WriterHand);
    m_Parked = true;
    m_HomeTarget = m_ParkTarget;               // 入口泡泡和退出泡泡共用 m_HomeTarget，照旧
}
else
{
    if (m_Parked) { m_Parked = false; m_UnlockAt = Time.time; m_HomeArmed = false; }
    // 入口泡泡的 m_PortalArmed 在 nested 自己的 SetPortalGuard 里，从锁住变为不锁时设回 false 并记录 m_UnlockAt
    // 原来的 60° 重新定位逻辑不变，然后：
    if (m_Play != null && m_Play.TryChimeKeepOut(out var keep))
        m_HomeTarget = ChimeFocus.AvoidKeepOut(eye, m_HomeTarget, keep, ChimeFocus.KeepOutRadius, .08f);
}
```
- 平滑：让位时插值速度按 0.6 秒到位，回位按 0.8 秒；原来是 `1 - exp(-dt*5)`，可以保留同样写法，换成对应常数。
- **入口泡泡**（nested，Phase.Portal）：
  - 新增 `public void SetPortalGuard(bool locked, float scale, float alpha)`。外层每帧调用：`m_Nested.SetPortalGuard(focus, focus ? ChimeFocus.ParkScale : 1f, focus ? ChimeFocus.ParkAlpha : 1f)`。
  - 在 nested 的 Portal 分支里：
    ```csharp
    var touching = FingertipTouching(m_Root.transform.position, m_Radius);
    if (m_PortalLocked) { if (m_Hold.Update(touching, Time.deltaTime)) EnterFromPortal(true); UpdateHoldRing(m_Hold.Progress); return; }
    m_Hold.Reset(); UpdateHoldRing(0);
    if (Time.time - m_UnlockAt < ChimeFocus.UnlockGraceSeconds) { m_PortalArmed = false; return; }
    // 原来的 armed 逻辑
    ```
  - 半径用 `SetRadius(.08f * scale)` 平滑过渡；透明度改 membrane 材质的 alpha（`BubbleWorldData.SetAlpha(m_MembraneMaterial, m_MembraneAlpha * alpha)`）。
- **退出泡泡**（`m_Home`）：
  - `m_Home.transform.localScale = Vector3.one * .16f * scale`，`m_HomeMaterial` 的 alpha 乘 `alpha`；
  - 锁住时 `m_Home.GetComponent<Collider>().enabled = false`；
  - 指尖逻辑同上，用 `m_HomeHold` 按住 0.6 秒后 `RequestHome(true)`。
- **集中把关**：`EnterFromPortal(bool force = false)` 和 `RequestHome(bool force = false)` 开头都加一句：被锁且 `!force` 就 `return`。这样不管从哪条路径触发（指尖、注视捏合、编辑器鼠标、以后新加的入口），都拦得住。
- **按住光环** `UpdateHoldRing(progress)`：
  - 外观：泡泡前方一圈细环，半径 = 泡泡半径 × 1.15，线宽 3 mm，始终面向人；颜色用风铃同色系（糖果 `#FFB3D1`、自然 `#D9FF8A`），不透明。
  - 实现：程序化网格，48 段，按进度画出对应弧长；进度变化超过 1/48 才重建网格。
  - 进度为 0 时隐藏。
  - 用 Unlit 材质（`BubbleWorldData.CreateUnlit`），**不要**用 LineRenderer。
  - 放进对应的 `VisionOSSortingGroup`，Order 比泡泡膜高一级。
- 原来传给 `BubbleWorldPlay.Initialize` 的 `exclusion`（涟漪不在泡泡旁触发）保持不变。

### 日志

关键节点各打一行，方便真机看：
```
[Bubble Play] chime focus on (writing|flying|reach) writer=L|R|?
[Bubble Play] follow bubble parked at yaw=-50 locked
[Bubble Play] locked bubble hold 0.60s → enter|home
[Bubble Play] chime focus off, bubble returning (grace 0.8s)
```

---

## 测试

`WishChimePolishTests.cs` 已写好，覆盖：
- 贴图路径；
- 状态优先级；
- 22 张贴图都能加载且尺寸正确；
- 按钮和纸条的空隙；
- 停靠在空闲手一侧；
- 停靠点避开纸条和按钮；
- 默认位置会被推出禁区，推开后距离和高度不变；
- 不在禁区时不动；
- 推开方向保持原来那一侧；
- 专注的触发条件和 1.2 秒尾巴；
- 按住手势必须连续。

请再补以下测试（可以用现有的测试替身，或把判断抽成纯函数）：
1. 锁住时 `EnterFromPortal()` / `RequestHome()` 不带 force 不生效，带 force 生效。
2. 解除专注后 0.8 秒内指尖碰到泡泡不触发；之后要先离开再碰才触发。
3. `PressBlocked`：正在画笔划、笔划结束 0.35 秒内、擦除中、保存中，按钮都不响应。
4. 没写字时放飞按钮是 disabled，按下不调用 `ReleaseWish`。

跑全量 EditMode。和 bubble-play-1031 的基线（695 项、29 项已有失败）对比，不应新增失败。

---

## 真机验收

1. **按钮**：四个世界都看一遍，重写 / 放飞文字清楚、不发虚。棉花糖第一层和雪原最亮，重点看这两个。玻璃太淡就用 `python3 Source/tools/export_wish_buttons.py --boost 6` 重新导出。
2. **按钮防误触**：在纸条左右边缘快速写字，手扫过按钮旁边时不会误按重写或放飞。
3. **让位**：空白风铃出现时，泡泡自动在侧边，不和风铃重叠。伸手去写时泡泡滑到不写字那只手那侧并变小变淡。
4. **上锁**：写字时手、手腕故意穿过泡泡，不会进下一层或退出；看着泡泡捏合写字也不会。
5. **解锁离开**：写到一半手指按住泡泡，光环 0.6 秒填满后正常进入或离开；按 0.3 秒就拿开，什么都不发生。
6. **归位**：放飞后风铃挂好，约 1.2 秒后泡泡滑回原位；手正好停在泡泡位置时不会马上触发。
7. **两层都测**：外层（下一层入口泡泡）和第二层（退出泡泡）都要测。

## 交付

在 `docs/verification/` 里写一份记录，内容包括：
- 测试数量和结果；
- 四个世界的按钮截图；
- 编辑器里模拟 "写字中泡泡停靠" 的截图。

先不要推送，等用户确认。
