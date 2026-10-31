# Moodium 泡泡世界讲解 + 呼吸声（2026-10-30）

这个包有两部分：

1. **泡泡世界讲解**：每进入一层泡泡世界，Moodi 会用治愈的语气介绍这里。一共 4 段，中英文各一套。
2. **呼吸声**：新做的一对吸气 / 呼气音频，给呼吸模式用。

## 一、泡泡世界讲解

### 文案

| 编号 | 世界 | 中文 | English | 时长 中 / 英 |
|---|---|---|---|---|
| W1c | 糖果 · 第一层（棉花糖） | 欢迎来到棉花糖的世界，软软的，像小时候的云朵。在这里，做回那个小小的自己吧，把今天的累，轻轻放下～ | Welcome to the marshmallow world, soft like the clouds of your childhood. Here, you can be your little self again. Gently set today's tiredness down. | 13 s / 14 s |
| W2c | 糖果 · 第二层（薄荷巧克力） | 欢迎来到薄荷巧克力的梦。清凉的薄荷，提醒你放慢呼吸；浓浓的巧克力，提醒你，你值得被好好照顾。今天的你，已经很棒啦～ | Welcome to the mint chocolate dream. Cool mint reminds you to slow your breath. Rich chocolate reminds you that you deserve to be cared for. You've done so well today. | 18 s / 17 s |
| W1n | 自然 · 第一层（森林） | 欢迎来到绿色的森林。这里的每一片叶子都在呼吸，空气里满满的都是氧气。放松肩膀，深深吸一口，把森林的清新留在身体里吧～ | Welcome to the green forest. Every leaf here is breathing, and the air is full of fresh oxygen. Relax your shoulders, breathe in deep, and keep the forest's freshness inside you. | 15 s / 20 s |
| W2n | 自然 · 第二层（雪原树林） | 我们来到了安静的雪原树林。好冷呀，雪花轻轻落下，整个世界都慢了下来。听，什么声音都没有，只有你的呼吸。就在这里，静静地待一会儿吧～ | We've arrived at a quiet, snowy forest. Brr, it's cold. Snow falls softly, and the whole world slows down. Listen. No sound at all, only your breath. Stay here, quietly, for a little while. | 20 s / 26 s |

### 配音

- 用的是 Moodi 的音色：MiniMax `speech-2.8-hd`，Cute Spirit，和贴纸邀请、散步陪伴是同一个声音。
- 正常语速（1.0）。
- 每句之间停顿 0.8 秒。
- 格式：WAV，44.1 kHz，单声道，响度约 -18 LUFS，和 Moodi 其他语音一致。
- 文件在 `Unity/Resources/MoodiumVoice/{zh,en}/moodi_vo_W*_{zh,en}.wav`。

### 什么时候播

- 每一层泡泡世界完全展开后（点云和天空都出现了），再等 1 秒开始讲解。
- 进入第二层时，如果第一层的讲解还没讲完，会淡出，换成第二层的讲解。
- 每层每次进入世界只讲一次。回到房间再进泡泡，不会重复讲。
- 原来第一层展开后 1.5 秒说的 F3，改成等讲解结束后再说。
- 握袋呼吸的提示 F4，也等讲解结束后才说。
- 走出泡泡范围（F5）或离开泡泡时，讲解会在 0.5 秒内淡出。
- 讲解是 2D 声音，像在耳边说话。泡泡里 Moodi 是隐藏的，所以没有用 Moodi 身上的 3D 声源。
- 语言跟随 `PlayerPrefs "Moodium.Language"`，和 Moodi 其他语音一致。

### 代码改动

基于 `codex/nested-footprint-1030` 分支的 `41425c2`（也就是点云重新导出并烘焙之后的版本）。

- `BubbleWorldController.cs`：新增事件 `LayerRevealed(string folder)`。第一层和第二层展开时都会触发，第二层由外层转发。原有的 `Revealed` 不变。
- `BubbleWorldNarrator.cs`（新增）：播放讲解的组件，自带一个 2D 声源。
- `CandyWorldManager.cs`：订阅 `LayerRevealed` 后播放讲解。F3、F4 等讲解结束再说；F5 和离开泡泡时淡出讲解。
- `BubbleWorldNarratorTests.cs`（新增）：5 个 EditMode 测试。

### 接入步骤

1. 在仓库根目录执行：
   ```bash
   git apply Unity/Patches/moodium-bubble-world-voice-1030.patch
   ```
   - 已核对：补丁能在 `41425c2` 上干净地打上。
   - 不想打补丁的话，也可以手动拷贝 `Unity/Scripts`、`Unity/Editor`、`Unity/Resources` 里的文件。
2. 对 `Assets/Resources/MoodiumVoice` 右键 → Reimport。
3. 跑 EditMode 测试 `BubbleWorldNarratorTests`。
4. 真机上进糖果世界和自然世界的泡泡，各进两层，听一下讲解的音量和时机。音量可以在 `BubbleWorldNarrator` 的 `m_Volume` 调，默认 0.9。

## 二、呼吸声（`Breath/`）

| 文件 | 时长 | 说明 |
|---|---|---|
| `breath_inhale.wav` | 4 秒 | 轻柔的吸气声，越往后越亮，像肺慢慢吸满 |
| `breath_exhale.wav` | 5 秒 | 放松的"哈——"呼气声，偏暖，慢慢淡出 |
| `preview_two_breaths.wav` | — | 吸、呼连起来放两轮，只用来试听 |
| `make_breath_v2.py` | — | 生成脚本（numpy），改参数可以重新生成 |

- 格式：单声道，44.1 kHz，16 位 WAV。
- 文件名和呼吸声补丁（`Moodium-BreathSound-1030.zip`）里的一样。先接好那个补丁，再用这两个文件覆盖 `Assets/UI/Moodium/SqueezeModes/Resources/MoodiumSqueezeModes/` 下的同名文件即可，代码和 `.meta` 都不用改。

## 其他文件

- `Source/minimax/`：MiniMax 原始整段录音和合成用的文本。`v1/` 是第一版（旧糖果文案，0.9 倍语速），只留作对比。
- `Source/split_worlds.py`：把整段录音按 3 秒长停顿切成 4 段，并统一响度。

## 还没验证

- 代码没有在 Unity 里编译过，测试也没跑过（这台机器没有 Unity）。
- 没上真机听过。背景音乐会一直播，音量 0.35，讲解可能需要调大，或者在讲解时把音乐压低一点。
- MiniMax 网页的情绪标签没法用，所以用的是 Cute Spirit 的默认语气。
