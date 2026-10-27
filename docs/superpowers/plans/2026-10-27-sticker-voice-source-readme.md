# Moodi 贴纸邀请语音（中英双语）

扫到贴纸、CandyWorldUI 或 NatureWorldUI 出现时，入口上的 Moodi 会开口说一句简短的邀请，吸引用户进入世界。

> **前提：先接好「Moodi 陪你散步」包（Moodium-MoodiVoice.zip）。** 这个包是在它之上的增量：它要用到散步包里的 `MoodiVoice` 播放组件、`Assets/Resources/MoodiumVoice/` 语音文件夹和导入设置。

## 三句台词

| 编号 | 什么时候 | 中文 | English |
|---|---|---|---|
| S1c | 糖果世界入口出现 | 来糖果世界玩吧，甜甜的哦～ | Come play in Candy World — it's sweet! |
| S1n | 自然世界入口出现 | 来自然世界，一起听听风吧～ | Come to Nature World — let's listen to the wind! |
| S2 | 出现的正是心情测试推荐的世界（代替 S1c / S1n）；或心情测完、推荐标签出现在某个入口上 | 今天的你，会喜欢这里哦～ | I think you'll love it here today! |

音色和散步包一样：MiniMax `speech-2.8-hd`，Cute Spirit，情绪 Happy，WAV 44.1 kHz 单声道。响度和散步包的语音一致（约 -18 LUFS）。

## 包里有什么

| 路径 | 放到工程里的位置 | 说明 |
|---|---|---|
| `Unity/Resources/MoodiumVoice/zh/moodi_vo_S*_zh.wav` | `Assets/Resources/MoodiumVoice/zh/` | 3 条中文（新增） |
| `Unity/Resources/MoodiumVoice/en/moodi_vo_S*_en.wav` | `Assets/Resources/MoodiumVoice/en/` | 3 条英文（新增） |
| `Unity/Patches/StickerWorldSelectionController.diff` | — | 贴纸入口出现时让 Moodi 说话 |
| `Unity/Patches/MoodiVoice.diff` | — | `MoodiVoice` 和 `MoodiVoiceTests` 的增量改动 |
| `Unity/Scripts/StickerWorldSelectionController.cs` | `Assets/Scripts/Moodium/Flow/`（覆盖） | 打好补丁的完整文件 |
| `Unity/Scripts/MoodiVoice.cs` | `Assets/Scripts/Moodium/Characters/`（覆盖） | 打好补丁的完整文件 |
| `Unity/Editor/MoodiVoiceTests.cs` | `Assets/Editor/`（覆盖） | 打好补丁的完整文件 |
| `Unity/Editor/MoodiStickerInviteTests.cs` | `Assets/Editor/` | 新增测试 |
| `moodi_sticker_voice_lines.csv` | — | 这 3 句的台词表 |
| `Source/minimax/` | 不进工程 | MiniMax 原始整段音频和合成用文本 |

## 接入步骤

1. 确认散步包已经接好（工程里有 `MoodiVoice.cs` 和 `Assets/Resources/MoodiumVoice/`）。
2. 拷入 6 个 WAV 和 `MoodiStickerInviteTests.cs`。
3. 打补丁，二选一：
   - 在仓库根目录运行 `git apply Unity/Patches/MoodiVoice.diff Unity/Patches/StickerWorldSelectionController.diff`；
   - 或者用 `Unity/Scripts/`、`Unity/Editor/` 里的完整文件覆盖那三个文件。
   - 已核对：在「仓库 c763fa30 + 散步包」上两个补丁都能干净地打上，结果和完整文件一致。
4. 对 `Assets/Resources/MoodiumVoice` 右键 → Reimport。
5. 跑 EditMode 测试：`MoodiStickerInviteTests`、`MoodiVoiceTests`、原有的 `StickerWorldSelectionControllerTests`。

**不用改场景和预制体。** 说话的是入口里原有的 Moodi（带 `MoodiCompanionAnimator` 的 Moodi meshy），`MoodiVoice` 会在第一次说话时自动加上。

## 播放规则

- 入口出现 0.6 秒后开口，和 Moodi 原有的挥手同步。
- 两种情况算「出现」：第一次扫到贴纸生成入口；贴纸丢失后重新出现。
- 心情测完、推荐标签出现时，推荐世界的 Moodi 在原有的开心转圈（Happy）时说 S2。
- 两张贴纸同时在视野里时，4 秒内只有一个 Moodi 说话。
- 同一个世界 20 秒内不重复说（推荐标签那次除外），所以贴纸短暂丢失再找回不会反复说。
- 语言跟随开场选择的语言（`OpeningVideoSession.English`），不是 PlayerPrefs。
- 入口被隐藏时（贴纸丢失、打开心情测试），还没说出口的话会丢掉，重新出现时不会说旧话。
- 入口里没有 `MoodiCompanionAnimator` 时不说话，也不报错。

## 改了什么

`StickerWorldSelectionController`：
- 新增公开静态方法 `MoodiInviteLine(worldId, recommendedWorldId)`：返回该说哪句，有测试。
- 新增公开静态方法 `CanMoodiInvite(...)`：判断 4 秒和 20 秒的冷却，有测试。
- 新增私有方法 `MoodiSays`，和冷却计时用的两个字段。
- `HandleTrackedImageUpdate`：入口新生成或从隐藏变为显示时，调用 `MoodiSays`。入口的生成、隐藏、移除逻辑都没变。
- `UpdateRecommendationTag`：在原有的 `PlayHappy()` 之后调用 `MoodiSays(entry, "S2", true)`。

`MoodiVoice`（相对散步包里的版本）：
- `AllIds` 加上 S1c、S1n、S2（共 28 句）。
- 新增 `EnglishOverride`：单个 Moodi 可以指定语言，不设时仍跟随 PlayerPrefs。
- 新增 `SayAfter(id, 秒)`：延迟说。
- 新增 `OnDisable`：Moodi 被隐藏时清掉排队和延迟的句子。

`MoodiVoiceTests`：语音总数 25 改为 28，加一条贴纸台词的测试。

## 还没验证

- 代码没有在 Unity 里编译过（这台机器没有 Unity 和 C# 编译器）。拷进去后先看 Console。
- 没上真机听过。入口上的 Moodi 很小，声音从它身上发出，音量可能要调 `MoodiVoice` 的 `m_Volume`。
- 「贴纸丢失后重新出现」依赖 ARFoundation 在丢失时报 `TrackingState.None`。如果 visionOS 丢失时只报 `Limited`，就只有第一次出现时会说。
