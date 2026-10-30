# 真机五个问题：手腕 UI、开机视频循环、泡泡入口延迟、Moodi 交互、手部尾迹（2026-10-30）

基准：`codex/bubble-hand-wake-1030` 的 `f56a589`（GitHub 最新，含面板修复和手部尾迹）。

**这份改动在一台没有 Unity 的机器上写成，没有编译，也没有跑测试。** 下面的"原因"都是读代码得出的，标了"推断"的需要真机日志确认。

## 1. 泡泡世界里不要显示手腕 UI

**原因**：手腕光带 `CandyEnergyWristUI` 只看手有没有被追踪到，没有任何"在泡泡世界里"的判断。

**改法**
- `CandyEnergyWristUI` 新增 `Suppressed`：
  - 为 true 时光带淡出，不再响应抬腕查看，按钮不可点；
  - 完全淡出后，关掉光带、卡片、提示三块显示，插槽里的宝石也一起隐藏；
  - 手部追踪照常运行，`HandTracked` 仍然有效，供 Moodi 等其他组件使用。
- `CandyWorldManager.Update` 每帧设置：`Suppressed = 正在运行 && 旅程阶段 == Bubble`。
  - 阶段从"碰泡泡"那一刻进入 Bubble，回到房间时恢复为 Explore。
  - 所以从碰泡泡开始光带淡出，回到房间后再出现。纯函数是 `WristHiddenFor`。

## 2. 跳过开机动画后，"请对准贴纸"时透明底视频卡在一帧

循环段是 `OpeningVideoTimeline.StickerSelectionLoop`，第 1240–1380 帧（0:41:10–0:46:00）。

### 原因

**① 主要原因：刚跳转后的旧状态让循环一直重新跳转**

旧代码 `StickerWorldSelectionController.Update` 的逻辑是：只要 `frame >= 1380` 或 `!isPlaying`，就 `Pause()`、`time = 1240`、等 `seekCompleted`、`Play()`，只留一帧缓冲，然后再检查。

visionOS 上，跳转之后 `frame`、`isPlaying` 有一段时间还是旧值，工程里别处的注释也提到过这一点。

- **从循环段之后跳过**（硬件教学的大部分、英文分支）：`frame` 仍然显示 > 1380，于是每一两帧就重新跳转一次。画面一直停在暂停状态，最稳定地卡死。
- **从循环段之前跳过**（开头、语言选择）：只有 `isPlaying` 滞后时才会触发，所以是"容易"卡，不是每次都卡。

**② 开机流程里自己的跳转还在进行中**

开头预览循环和语言循环会定期跳转。跳过时没有取消这些跳转，旧跳转的 `seekCompleted` 先到，被误当成新跳转已完成。

**③ 先 `Pause()` 再跳转**

AVFoundation 暂停时可能不显示目标帧，也不发 `seekCompleted`，每次都要等满 3 秒超时。

**④ 视频贴图更新器在隐藏后丢失贴图（确定的 bug）**

`PolySpatialVideoRenderTextureUpdater.OnDisable` 把贴图引用清空，而 `SetVisible(true)` 之后不会再设置回来。

所以只要视频被隐藏过一次，再回到选关时，Vision Pro 上会一直显示最后一帧，声音和视频其实还在走。会隐藏视频的情况：进过世界、走过无感官袋的转盘、在设置里切换过感官袋。

### 改法

- 新增纯逻辑 `VideoSegmentLoopDriver`（不调用任何视频接口，可以在 EditMode 测试）：
  - 跳转只有在**真的看到起点附近的帧**（1237–1254）时才算完成，不信 `seekCompleted`，也不信旧的帧号。
  - 只有确认已经在循环段里之后，播到 1380 才回到起点。
  - `isPlaying` 短暂为 false 不处理；持续 0.5 秒以上只补一次 `Play()`，不跳转。
  - 看门狗：帧号 1.2 秒不前进，先 `Play()`；再过 1.2 秒还不前进，重新跳转。
  - 跳转 2.5 秒没落地：先 `Play()`，再等 1.2 秒，然后重新跳转。
- `StickerWorldSelectionController`：
  - 删除旧的协程；
  - 跳转改为 `player.frame = 1240; player.Play()`，**不再先 `Pause()`**；
  - 播放器还没准备好时，先 `Prepare()`，准备好后再跳转；
  - 每帧把帧号交给驱动器，按返回结果执行 `Play` 或 `Seek`；
  - 编辑模式下保留原来的直接 `time = …; Play()`，旧测试不受影响。
- `PolySpatialVideoRenderTextureUpdater`：删除 `OnDisable` 里清空贴图的代码。贴图销毁后，`ShouldTransfer` 本来就会返回 false。

## 3. 下一层泡泡入口和泡泡出口要等几秒才出现

### 原因

1. **写死了 10 秒等待。** `BubbleWorldController.Update` 里有 `if (Time.time - m_InsideAt < 10f) return;`，对下一层入口和出口泡泡都生效。
2. **下一层进去之后才开始加载。** `PrepareNested()` 只在进入 Inside 后才调用，要加载 50 万点的数据和 20 层网格，又要等几秒。
   - 离开世界时 `Return()` 会销毁下一层，所以再次进入还要重新等。

### 改法

- **等待时间可调。** 新增 `m_NextBubbleDelaySeconds`，默认 0，即一进入世界就出现。
- **提前加载下一层。** 新增 `m_PrepareNestedEarly`，默认开：
  - 第一层自己建完后（人还在房间里），立刻开始建下一层；
  - 从世界回到房间后（`Return` 结束），也立刻重建下一层；
  - 进入 Inside 时的原调用保留，作为兜底。
- **防误触（重要）。** 下一层入口和出口泡泡出现的位置（眼前 0.6 m、下方 0.25 m），正好是刚才手指点泡泡进入的位置。原来等 10 秒，很可能就是为了避开这个问题。
  - 现在泡泡立即出现，但手指要先离开它一次（`m_PortalArmed` / `m_HomeArmed`），之后的触碰才算数。
  - 手停在原处不会被误触而直接进入下一层或退出。
  - 用 pinch 或点击按钮（`MoodiumSpatialButton`）不受这个限制。

### 代价

两个世界的点云在房间阶段就同时在内存里。原来进入第一层后本来也会同时存在两个，所以峰值内存不变，只是时间提前了。下一层是分帧建的，房间阶段可能有轻微卡顿；如果真机上明显，把 `m_PrepareNestedEarly` 关掉对比。

## 4. Moodi：手心里的触摸和待机动画消失；选关界面的 Moodi 没反应；摸的时候会往下掉、变小

### 以前的版本（`06b1f14` 到 `26bb73d`）

- **触摸**：`MoodiCompanionAnimator.CheckHandTouch` 用手部关节（食指尖、中指尖、手掌）检测是否碰到身体球体。
  - 碰到后播 `Touched`：0.9 秒的压扁回弹。
  - 在手心上每拍 3 次：`PlayHappy()`（转圈）加一句语音。
- **待机**：`Idle` 循环（3 秒呼吸）。

### 现在为什么没有了

`236b4d7` 把 Moodi 从手腕移到手心（点位更靠近指根，离手心 7.5 cm）；之前 `195db9a` 把 Moodi 从 4 cm 放大到 20 cm。

**a. 托着 Moodi 的那只手卡住了触摸检测**
- 两只手共用一个"在里面"的标记。
- 20 cm 的 Moodi，触摸球半径约 8.6 cm。托着它的那只手，指尖经常就在球里，标记一直是 true，另一只手去摸时永远没有"刚碰到"的那一下。
- 指尖在球边缘抖动时，又会随机误触发。

**b. 待机被走路动画顶掉**
- 动画根据 Moodi 根物体的水平速度决定 Idle 还是 Walk（> 0.08 m/s 就走路）。
- Moodi 在手心上时，根物体跟着手动，正常的手部晃动就让它一直播走路的小跳，看不到 Idle 呼吸。

**c. 两个脚本抢模型缩放**
- `MoodiBreathingFloat` 在 Moodi 生成的那一刻记下了原始缩放，之后**每帧**都把缩放写回原始值。
- 而 `PrepareGuide` 把模型缩放到 20 cm，手心脚本的落地压扁也在写这个缩放。
- 两边每帧互相覆盖，表现为 Moodi 忽大忽小、落地压扁看不到。这很可能是"摸的时候变小"的来源之一（推断）。

**d. 摸的时候往下掉（推断）**
- 另一只手伸过来时，visionOS 常常短暂丢失托着的那只手，或者让它的关节跳动。
- 这会触发松手的条件之一：手丢失 0.4 秒、手心翻转 0.25 秒、或者被判断为"往上抛"。
- 一旦松手，Moodi 就飞走，回到"眼前 0.65 m、头下 0.45 m"的跟随点，看起来就是从手里掉下去、变小了。

**e. 选关界面**
- 以前的 `MoodiSelectionMascotController`（ShySmile / Wink 交替）在 `ddf067c` 换成新骨骼 Moodi 时被有意停用，因为旧动画片段会让新骨骼卡住。
- 新 Moodi 只剩一个很小的压扁动画，在几厘米高的模型上几乎看不出来。
- 转盘转动时，它还会被判断成在走路。

### 改法（保留手心位置）

- **`MoodiCompanionAnimator`**
  - 每只手单独判断"刚碰到"（纯函数 `TouchStarts`）。
  - 新增 `SetSupportHand(0/1/-1)`：托着 Moodi 的那只手不算触摸。
  - 新增 `ConfigureSelectionTouch()`，用于选关界面：
    - 固定 Idle，不因为转盘移动而走路；
    - 触摸范围多 3 cm；
    - 触摸时交替播 `Happy`（转圈）和 `Wave`（挥手），对应以前 ShySmile / Wink 的交替。
- **`MoodiHandCompanion`**
  - 落到手心时调用 `SetWalking(false)` 保持 Idle，并调用 `SetSupportHand(托着的那只手)`；松手和复位时清除这两项。
  - 被摸后 1.2 秒内，或者另一只手的手心在 Moodi 身体 16 cm 以内时：
    - 不触发"翻手松开"和"上抛松开"；
    - 托着的手丢失追踪时，可以等 1.5 秒（平时是 0.4 秒）。
  - 拍 3 次转圈加语音的逻辑不变。
- **`WalkTrailGuideController`**：在缩放到 20 cm **之前**关掉手心 Moodi 身上的 `MoodiBreathingFloat`。关掉时它会恢复原始缩放，所以必须在缩放之前关。手心上下浮动本来就由手心脚本负责。
- **`MoodiSelectionMascotController.AttachTo`**：遇到新骨骼 Moodi 时调用 `ConfigureSelectionTouch()`。和以前一样返回 null，旧测试的期望不变。转盘和贴纸选关两条路径都经过这里。

### 没有改的

- 手心 Moodi 的大小（20 cm）和位置。
- `Touched` 动画片段本身（就是以前的那段压扁回弹）。

## 5. 泡泡世界伸手没有反应（手部尾迹）

### 原因（推断，代码路径本身能跑通）

- **触发门槛太高**：手速要超过 0.25 m/s 才开始生成，1.2 m/s 才满量。伸手、慢慢移动几乎没有。
- **点太小**：只有 4–9 mm，生成后很快缩小消失，在真机上很难注意到。
- **找不到颜色就不生成**：手背后方向如果没有世界点（比如天空），就不生成。
- **排序没有定义**：尾迹挂在泡泡根物体下，不在点云的排序组里，和排序组之间的前后顺序没有定义。
- **没有日志**：无法判断是哪一步没起作用。

### 改法（`BubbleHandWake`、`BubbleWorldController`）

- **更容易触发**：起始手速 0.06 m/s，满量 0.7 m/s，最多每秒 180 个。
  - 新增 `IdlePerSecondPerHand = 14`：手在视野里不动，也会冒出零星小点，所以伸手就有反应。
- **点更大**：改为 7–14 mm。只影响尾迹点，**世界点云的点大小不变**。
- **颜色兜底**：手背后找不到点时，先在更大范围（24 格）里找，再从整个世界随便取一个点的颜色（`PickAny`）。
- **加入排序组**：尾迹改挂在扫描根物体下，并登记进点云的排序组，顺序 2，排在点云之后。
  - 方片尺寸按缩放修正，所以呼吸缩放时大小也是对的。
- **日志**：每次激活状态变化时，以及激活期间每 5 秒，打一行：
  ```
  [Bubble Wake] active=… lookup=… hands=… L=… R=… spawned=… live=… renderer=…
  ```
  真机上如果还是没反应，看这一行就能知道是没激活、没有手、没生成，还是生成了但看不见。

## 6. 测试

| 测试 | 内容 |
|---|---|
| `VideoSegmentLoopDriverTests`（新） | 跳转后的旧帧号被忽略；跳转丢失时先 Play 再重新跳转；只有落地后才在结尾循环；短暂不播放时等待，长时间不播放只补 Play；帧不前进时先 Play 再跳转；跑出循环段时跳回 |
| `RealDevice1030bTests`（新） | 手腕只在泡泡世界里隐藏；隐藏时停止输入；入口不再等 10 秒且需要手指先离开；视频贴图更新器隐藏再显示后仍有贴图；选关循环不再 `Pause()`；`PickAny`；托着的手不阻挡也不误触发触摸；选关 Moodi 配置；手心 Moodi 保持 Idle、被摸时不掉；呼吸浮动在缩放之前关闭 |
| 现有 `MoodiAnimatedTests`、`StickerWorldSelectionControllerTests`、`BubbleHandWakeTests`、`BubbleWorld*`、`IntroVideoGateTests`、`OpeningSkipBehaviorTests` | 必须仍然通过 |


## 本地实施与验证

从原工程当前 f56a589 创建 codex/realdevice-fixes-1030b，输入补丁原样提交 4c06ccf。代码实际修改在 `/Users/yibei/Desktop/appcontest2026/Moodium`；独立副本仅用于编译和自动化测试。未合并、未推送。工程已有 FBX 改动保留，未纳入本次提交。

Unity 6000.0.30f1 编译通过，无需修正输入补丁。新脚本 .meta 由 Unity 生成并复制回原工程。

全套 665 项：**636 通过、29 失败**。与 f56a589 对应的 bubble-hand-wake 全套 XML 按名称比较，新增失败 0、旧失败消失 0。[完整 XML](realdevice-fixes-1030b/full-tests.xml)、[29 项旧失败逐项清单](realdevice-fixes-1030b/remaining-failures.md)。

指定套件：

- VideoSegmentLoopDriverTests：6/6
- RealDevice1030bTests：9/9
- MoodiAnimatedTests：14/14
- StickerWorldSelectionControllerTests：9/12
- IntroVideoGateTests：18/18
- OpeningSkipBehaviorTests：3/3
- OpeningVideoSessionTests：2/2
- BubbleHandWakeTests：8/8
- BubbleWorld：36/36

原有 StickerWorldSelectionController 等旧失败仍保留，不能把相关套件整体描述为全通过。没有修改旧测试期望来凑通过数。

本次保留手心 Moodi 大小/位置、Touched 动作、视频素材与帧常量、世界点云大小及静态圆点材质、面板和 Soft 天空方案。尾迹的尺寸和触发参数按包内要求更新，尾迹排序登记到世界排序组。

以下设备验收尚未执行：

1. 两层泡泡中手腕光带淡出、无输入，退出后恢复。
2. 各个开机进度点跳过与英文分支落入 1240–1380 循环，隐藏再显示、进世界返回与感官袋切换后继续播放。
3. 入口/出口立即显示；指尖先离开后再次接触才能触发；重进仍有效；提前准备下一层的实际卡顿。
4. 手心 Idle、另一手触摸压扁回弹、三次拍击反馈、触摸不掉落、不抢缩放、翻手/上抛放走；两种选关的 Happy/Wave 交替。
5. 静止/慢移手部尾迹可见、景色颜色匹配、尺寸观感、退出清理和帧率。

自动化通过不等同于 AVFoundation、RealityKit 或真机手追踪已验收。若尾迹仍不出现，保留包内新增 [Bubble Wake] active=… 诊断日志用于定位。
