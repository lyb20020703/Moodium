# 摸袋子上的 Moodi：播放骨骼动画，不再缩小（2026-10-31）

## 问题

Moodi 趴在解压袋上（从袋子里探头、等你把它捏出来）时，用手摸它：
- 没有播放摸摸的骨骼动画 Touched；
- 而是往下压扁，看起来像缩小了。

## 原因

1. Moodi 趴在袋子上时，由 `MoodiRiseDisplay` 接管，并把 `MoodiCompanionAnimator` 关掉了。触摸检测和 Touched 动画都在 `MoodiCompanionAnimator` 里，所以这段时间摸它没反应。
2. 手碰到 Moodi 时也碰到了它身下的袋子，被当成"捏袋子"，驱动 Moodi 的 Strain（用力下压）动画层，Moodi 就被压扁了。

## 改了什么

基于 `codex/nested-footprint-1030` 分支的 `41425c2`。

**`MoodiCompanionAnimator.cs`**
- 新增公开方法 `PollHandTouch(out bool handOnMoodi, float extraMargin = 0)`。组件被关掉时，其他脚本也能用它检测手有没有摸到 Moodi。
- 原有的触摸逻辑改为调用同一段代码。自由活动的 Moodi 行为不变。

**`MoodiRiseDisplay.cs`**
- Moodi 趴在袋子上时，每帧检测手有没有摸到它，判定范围比平时多 1.5 cm（`m_TouchExtraReach`），因为这时 Moodi 只有 4 cm 大。
- 摸到时，播放 Touched 骨骼动画，播完回到原来的探头或冒出状态。
  - 播放期间会暂时关掉 Peeking / Emerging 两个开关，否则这两个状态会立刻把动画打断。
- 手在 Moodi 身上、或 Touched 正在播放时，不再触发下压动作，Moodi 不会缩小。
- 真正捏袋子（手不在 Moodi 身上）时，Moodi 照常有用力的反馈。

**`MoodiSoftTouchTests.cs`**（新增）：4 个 EditMode 测试。

## 接入步骤

1. 在仓库根目录执行：
   ```bash
   git apply Unity/Patches/moodium-moodi-soft-touch-1031.patch
   ```
   - 已核对：能在 `41425c2` 上单独打上，也能在打过泡泡世界讲解补丁（`Moodium-BubbleWorldVoice-1030.zip`）之后打上。
   - 也可以直接用 `Unity/Scripts`、`Unity/Editor` 里的完整文件覆盖。
2. 跑 EditMode 测试：新增的 `MoodiSoftTouchTests`，以及原有的 `MoodiAnimatedTests`、`WorldFix1030Tests`。
3. 真机检查：
   - Moodi 掉进袋子、探出头后，用指尖碰它：应该播放摸摸动画，不缩小，播完继续探头；
   - 捏袋子其他位置：Moodi 仍有用力的反馈，能量照常增加；
   - Moodi 被捏出来以后，摸它照常有反应。

## 注意

- 判定范围是以 Moodi 身体为中心的小球。手掌压在 Moodi 正上方的袋子上，也会被算成"摸 Moodi"，不会压扁它。如果真机上觉得太灵敏或不够灵敏，调 `MoodiRiseDisplay` 的 `m_TouchExtraReach`，范围 0–4 cm。
- 摸 Moodi 时袋子本身仍可能算一次捏，能量照常增加，这里只改了 Moodi 的视觉反馈。
- 代码没有在 Unity 里编译过，测试也没跑过，真机上也还没试过。
