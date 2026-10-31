# 预设心愿 · 交接包（2026-10-31）

基于 `codex/wish-ink-visibility-1031`（8de6c1b）。**实现的 agent 请先读 `AGENT_TASK.md`。**

## 做了什么

手写不方便，所以在纸条左边加一列液态玻璃小签：五句写好的心愿，加一个「自己写」。

1. 点一句，这句心愿会自己写在纸条上：笔尖发光，按真实笔顺写，约 2.5–3.4 秒。
2. 写完风铃响一声，放飞按钮轻轻跳两下。
3. 再点放飞，和手写的风铃一样飞走、挂起。

| 中文 | English |
|---|---|
| 平安喜乐 | Peace and joy |
| 天天开心 | Be happy |
| 好事发生 | Good vibes |
| 身体健康 | Stay well |
| 烦恼退散 | Let it go |

布局：左边选内容，中间是纸条，右边是放飞（上）和重写（下）。

预设的字和手写存成同一种笔迹数据，渲染、保存、放飞、挂起的代码都不用改。

![书写中](Preview/writing_nature_zh_let_go_poster.png)
![十条预设写好后的纸条](Preview/presets_sheet.png)

完整流程视频：
- `Preview/writing_candy_zh_peace_joy.mp4`
- `Preview/writing_nature_en_stay_well.mp4`
- `Preview/writing_nature_zh_let_go.mp4`

同名 `.gif` 是缩小版。

## 目录

| 路径 | 内容 |
|---|---|
| `AGENT_TASK.md` | 实现说明：放哪、补丁、交互规则、数值、测试、真机验收 |
| `Unity/Resources/MoodiumBubblePlay/WishPresets/wish_presets.json` | 十条预设的笔划数据（80 KB） |
| `Unity/Resources/MoodiumBubblePlay/WishPresets/Chips/` | 56 张签的贴图（3 倍导出，透明底） |
| `Unity/Scripts/BubbleWorld/Play/` | `WishWritingTimeline`（书写节奏）、`WishPresetLibrary`（读数据）、`WishPresetPanel`（签列） |
| `Unity/Editor/` | 贴图导入设置、14 项 EditMode 测试 |
| `Unity/ThirdPartyNotices/WishPresets/` | 字形数据的许可说明 |
| `Patch/` | 对 4 个现有文件的补丁，以及打完后的完整文件对照 |
| `Preview/` | 演示视频、动图、截图 |
| `Source/tools/build_presets.py` | 由字形源数据生成笔划 json |
| `Source/tools/export_wish_chips.py` | 导出签贴图（需要 Chrome、ffmpeg） |
| `Source/tools/capture.mjs` + `Source/preview/stage.html` | 渲染预览图和演示视频（需要 Chrome、ffmpeg、Node 22+） |
| `Source/glyph-sources/` | 中文笔画中线、英文单线字体原文件 |

想换句子或加句子：
1. 改 `build_presets.py` 里的 `PRESETS`。
2. 中文要先把新字的 json 放进 `glyph-sources/hanzi/`，来源是 hanzi-writer-data。
3. 依次运行 `build_presets.py`、`export_wish_chips.py`、`capture.mjs sheet`。

## 状态

- 笔划数据、签贴图、代码、补丁、测试、实现说明、预览视频都已完成。
- 项目代码我没有改，由其他 agent 接入。
- 这里没有 Unity，也没有 C# 编译器，新代码**没有编译过**。补丁已确认能在 8de6c1b 上干净应用。
- 书写手感、真机上的字迹清晰度、字有没有镜像，需要上真机确认。
- 中文笔画数据使用 Arphic Public License，可以商用，但需要附上许可全文（已包含）。
