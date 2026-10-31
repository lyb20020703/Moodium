# 世界生长音效：从"玻璃碎裂"换成奇幻的生长声（2026-10-31）

## 问题

两个世界里，随着交互进度长出糖果屋、长出树时，播放的是 `Assets/Epic Toon FX/Sound/etfx_explosion_sparkle2.wav`。这是一个"爆炸 + 闪光"音效，听起来像玻璃碎掉，不适合"生长、出现"的感觉。

## 新音效

| 文件 | 用在哪里 | 时长 | 听起来 |
|---|---|---|---|
| `Audio/grow_candy.wav` | 糖果世界：大糖果屋、小糖果屋长出来 | 2.4 秒 | 一串钢片琴音往上爬，像撒了糖霜的闪光，底下垫着温暖的和弦，最后有几点细碎的亮晶晶 |
| `Audio/grow_nature.wav` | 自然世界：每棵树长出来 | 2.5 秒 | 先是一阵往上升的轻柔气息，像嫩芽钻出来，再接几声温润的木质铃音，最后是温暖的余音 |

- 上扬部分约 0.7 秒，和树 0.75 秒的生长动画对齐，后面是约 1.5 秒的余韵。
- 起音都很柔和，没有硬的撞击声，加了一点空间混响。
- 格式：单声道，44.1 kHz，16 位 WAV，不削波。
- 用 `make_growth_sounds.py`（numpy）生成，改参数可以重新生成。

## 改了什么

基于 `codex/nested-footprint-1030` 分支的 `41425c2`。

- `CandyWorldManager.cs`：
  - 树生长（`NatureForestGrowthController`）改用 `Resources/MoodiumGrowth/grow_nature`；
  - 糖果屋和小糖果屋（`CandyCompletionHouseController`）改用 `Resources/MoodiumGrowth/grow_candy`；
  - 找不到新文件时退回原来的音效，不会没声音。
- **不变的地方**：原来的闪光音效还用在雨滴奖励的触摸和泡泡的触摸上，这两处没改。
- `GrowthSoundTests.cs`（新增）：3 个 EditMode 测试。

## 接入步骤

1. 在仓库根目录执行：
   ```bash
   git apply Unity/Patches/moodium-growth-sound-1031.patch
   ```
   - 已核对：能在 `41425c2` 上单独打上，也能在泡泡世界讲解补丁、摸 Moodi 补丁之后打上。
   - 也可以手动操作：把 `Audio/` 里两个 WAV 拷到 `Assets/Resources/MoodiumGrowth/`，再用 `Unity/Scripts/CandyWorldManager.cs` 覆盖原文件。注意：如果已经打了泡泡世界讲解补丁，那个补丁也改过这个文件，这时只能用 `git apply`，不能直接覆盖。
2. 跑 EditMode 测试 `GrowthSoundTests`。
3. 真机上走到长出糖果屋和树，听一下音量。声音是在生长的位置播放的 3D 声音，音量 0.7，和原来一样。

## 还没验证

- 代码没有在 Unity 里编译过，测试也没跑过。
- 真机上还没听过。树会一棵接一棵长，每棵都会播一次。如果听多了觉得重复，可以再做两三个变体随机播放。
