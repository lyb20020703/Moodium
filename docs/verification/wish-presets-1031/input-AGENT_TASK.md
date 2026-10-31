# 心愿风铃：预设心愿 + 自动书写动画

基线分支：`origin/codex/wish-ink-visibility-1031` @ `8de6c1b`。在它上面新开分支，例如 `codex/wish-presets-1031`。
不要改点云、点大小、点材质，不要启用 GPU 运动着色器。

## 要做的事

现在用手指在纸条上写字能写，但不方便，很多人可能不想写。给用户一个轻量的选择：

- 纸条**左边**一列液态玻璃小签：五句写好的心愿 + 「自己写」。
- 点一句，纸条上之前的内容快速淡掉，这句心愿**自己写出来**：笔尖发光，按真实笔顺一笔一笔写，2.2–3.4 秒写完。写完风铃轻响一声，放飞按钮轻轻跳两下。
- 用户再点放飞，沿用现有的放飞流程。
- 点「自己写」或直接在纸上写，就是原来的手写。

| id | 中文（纸上竖排） | English（纸上逐行写） | 英文签上的字 |
|---|---|---|---|
| `peace_joy` | 平安喜乐 | Peace / and / joy | Peace & Joy |
| `happy` | 天天开心 | Be / happy | Be Happy |
| `good_vibes` | 好事发生 | Good / vibes | Good Vibes |
| `stay_well` | 身体健康 | Stay / well | Stay Well |
| `let_go` | 烦恼退散 | Let / it / go | Let It Go |

语言跟随 `MoodiVoice.LanguageCode`，和重写 / 放飞按钮一致。

预览：`Preview/writing_*.mp4`（完整流程）、`Preview/presets_sheet.png`（十条写好后的纸条）。
演示视频里的时间、版式、线宽和下面的代码是同一套数值。

---

## 关键设计：预设就是"笔迹"

预设心愿存成和手写**完全相同**的格式：`List<List<float[]>>`，每个点是纸上的 `[u, v]`，u 从左 0 到右 1，v 从下 0 到上 1。所以：

- 渲染用现有的 `WishInkMesh`，不加新的网格或材质。
- 保存用现有的 `WishChimeRecord.strokes`。只多一个可选字段 `preset`，旧文件照常读取。
- 放飞、挂起、LOD 都不用改。
- 书写动画就是按时间把笔划一段段喂给 `ink.Active()` / `ink.Completed()`，和手写的实时显示走同一条路。

数据来源：
- 中文：Make Me a Hanzi 的笔画中线（楷体笔顺）。
- 英文：EMS Felix 单线手写体。

两者的许可说明见下文"第三方许可"。

---

## 包里的文件

| 包内路径 | 放到工程 |
|---|---|
| `Unity/Resources/MoodiumBubblePlay/WishPresets/wish_presets.json` | `Assets/Resources/MoodiumBubblePlay/WishPresets/` |
| `Unity/Resources/MoodiumBubblePlay/WishPresets/Chips/*.png`（56 张） | `Assets/Resources/MoodiumBubblePlay/WishPresets/Chips/` |
| `Unity/Editor/WishPresetChipAssetPostprocessor.cs` | `Assets/Editor/` |
| `Unity/Editor/WishPresetTests.cs` | `Assets/Editor/` |
| `Unity/Scripts/BubbleWorld/Play/WishWritingTimeline.cs` | `Assets/Scripts/Moodium/BubbleWorld/Play/` |
| `Unity/Scripts/BubbleWorld/Play/WishPresetLibrary.cs` | 同上 |
| `Unity/Scripts/BubbleWorld/Play/WishPresetPanel.cs` | 同上（含 `WishPresetChip`） |
| `Unity/ThirdPartyNotices/WishPresets/`（`NOTICE.md`、`ARPHICPL.TXT`） | `Assets/ThirdPartyNotices/WishPresets/`（不要放进 Resources） |
| `Patch/wish-presets-existing-files.patch` | 改 4 个现有文件，见下文 |
| `Patch/after/*.cs.txt` | 打完补丁后 4 个文件的完整样子，只作对照 |
| `Source/` | 不进工程：数据生成、贴图导出、预览渲染脚本 |

顺序：
1. 放 PNG 和导入脚本，对 `Chips` 文件夹点一次 Reimport。
2. 放 json 和代码，打补丁。
3. `.meta` 由 Unity 生成，记得一起提交。

---

## 1. 现有文件的改动（补丁）

在仓库根目录：

```bash
git apply --3way /path/to/wish-presets-1031/Patch/wish-presets-existing-files.patch
```

补丁基于 `8de6c1b`，我在那个提交上确认过能干净应用。如果中间这几个文件又改过，冲突时按下面的说明手动合并，`Patch/after/` 里有完整结果可以对照。

### `WishChimeStore.cs`
`WishChimeRecord` 加 `public string preset;`：预设 id，手写为 null。旧存档没有这个字段，读出来是 null。

### `WishInkMesh.cs`
- 加 `SetWidth(float ink, float glow)`，以及 `InkScale` / `GlowScale` 两个只读属性。
- `Build()` 里的半宽改为乘这两个倍数，默认是 1，所以手写不变。
- 预设用 `WishPresetLibrary.InkWidth = 0.7`、`GlowWidth = 0.5`。原因是字形比手指写的字密：手写线宽下「喜」「康」的光晕会糊成一片，对照见 `Preview/presets_sheet_handwidth.png`。

### `WishActionButton.cs`
加 `Pulse()`：放飞按钮轻轻放大两次，每次 6%，共 0.9 秒。预设写完时调用。`OnDisable` 会把缩放复原。

### `WishChime.cs`
- **布局**：
  - 原来是重写在左、放飞在右，现在两个都在右边：放飞在上（+2.6 cm），重写在下（−2.6 cm）。
  - 左边（−12 cm）放签列：`WishPresetPanel.Create(...)`，挂在 `buttons` 下面，飞走时一起隐藏。
  - `AddAction` 增加一个 y 偏移参数。
  - 按左边选内容、中间纸、右边动作的顺序摆放。
- **新字段**：
  - `presets`、`preset`、`timeline`、`presetId`、`lang`、`presetStart`、`presetDone`、`presetCursor`。
  - 公开 `PresetId`、`PresetWriting`、`static bool AutoReleaseAfterPreset = false`。
- **新方法**：`PickPreset`、`StartPreset`、`StepPreset`、`CompletePreset`、`StopPreset`、`AutoRelease`、`WriteOwn`、`InviteWriting`。
- **`Erase(float seconds = .5f, Action then = null)`**：可以接一个"擦完以后"的回调。擦完会把线宽复位成 1。
- **`Update()` 改动**：
  - 预设书写时调用 `StepPreset()`。
  - 预设书写时屏蔽手写落笔、手的光标、抓铃铛摇晃重写。
  - 写字音效只在没有预设书写时才归零。
  - 光标显示条件改为 `visible || presetCursor`。
  - 重写按钮在 `PresetWriting` 时也可按。
- **`Rewrite()`**：清掉预设和选中状态。
- **`ReleaseWish()`**：预设还在写时，先瞬间写完（不响铃、不跳按钮）再放飞；记录里写入 `preset = presetId`。
- **进入写字时**预先解析预设 json（`_ = WishPresetLibrary.Document`），避免第一次点签时卡一下。
- **读取存档**：`record.preset` 不为空时先 `SetWidth(0.7, 0.5)`，再 `Completed()`，挂着的风铃和写时一样细。

---

## 2. 交互规则

| 情况 | 行为 |
|---|---|
| 点一句心愿 | 签变成选中（主题色 + ✓）。纸上有内容就先 0.3 秒淡出，然后开始书写。 |
| 书写中 | 笔尖光点（6 mm）跟着笔走，写字音效 0.22。手写落笔、抓铃铛都被屏蔽。 |
| 书写中点另一句 | 停下，淡出，写新的那句。 |
| 再点已选中的那句 | 书写中或写完后都忽略，避免误点擦掉后面加的签名。 |
| 书写中点重写 | 停下并擦掉，取消选中。 |
| 书写中点放飞 / 做"托起"手势 | 立刻写完，然后照常放飞。 |
| 写完 | 风铃响一声（`Ring()`），放飞按钮跳两下。 |
| 写完后用手指在纸上写 | 允许，比如签个名，加在预设后面，线宽同预设。`preset` 仍记录。 |
| 点「自己写」 | 纸上是预设就整张淡出擦掉，包括预设后面加的签名；纯手写的字保留。取消选中，纸中央的笔尖光点"呼吸" 1.2 秒提示从这里写，手指一靠近纸面就停止，光标交还给手指。 |
| `PressBlocked` | 签和按钮共用：正在画笔划、刚松手 0.35 秒内、擦除中、保存中，都不响应。 |
| `AutoReleaseAfterPreset = true` | 写完 0.6 秒后自动放飞，即"选了就直接放飞"。默认关闭，产品想要一键放飞时再打开。 |

### 书写节奏（`WishWritingTimeline.cs`，与 `Source/preview/stage.html` 一致）

- **停顿**：每一笔前停 0.03 秒，换字或换行前停 0.14 秒。超过 74 点被拆开的长笔划（kind 0）不停。
- **总时长**：`1.4 + 笔划总长 / 45 cm/s` 秒，限制在 2.2–3.4 秒之间。实际是 2.45–3.4 秒。
- **速度分配**：时间按每笔长度分配，长笔慢、短笔快。每笔最少 0.05 秒；点很多的字因此超时时，整体等比加快。
- **笔速**：每一笔起笔、收笔慢，中间快（`EaseAt`），始终向前。

想调快慢只改这个文件里的常量。

---

## 3. 第三方许可

- 中文笔画数据来自 Make Me a Hanzi，使用 **Arphic Public License**。可以商用和随 App 发布，但派生数据要附上许可全文。`ARPHICPL.TXT` 已在包里。
- 英文来自 EMS Felix，使用 **SIL OFL**。只用了笔画路径，不打包字体文件。

两份说明都放到 `Assets/ThirdPartyNotices/WishPresets/`，不要放进 Resources，否则会被打进运行时资源。App 里如果有"致谢 / 开源许可"页面，请加上这两项。

---

## 4. 日志

```
[Wish Preset] pick <id>/<lang> strokes=<n>
[Wish Preset] written <id>/<lang> points=<n>
```

---

## 5. 测试

`WishPresetTests.cs`（14 项）覆盖：

- **数据**：五句 × 两种语言都在，中文文字正确。
- **笔划规则**：每笔 2–74 点，坐标在 0–1，kind 合法，续笔首尾相接，总点数不到上限的四分之一（给签名留空间）。
- **版式**：中文四个字从上到下，英文每行一个词，避开顶部挂钩和自然世界的叶子。
- **拷贝**：取出的笔划是深拷贝，改了不会影响库里的数据。
- **存档**：`preset` 字段能存能读；没有这个字段的旧存档照常读取。
- **时长**：每句在 2.2–3.4 秒写完。
- **采样**：完成笔数只增不减，笔尖永不后退，结束时全部写完。
- **停顿**：停顿时长和 kind 对应，续笔不停。
- **缓动**：曲线单调，起笔慢。
- **签的状态**：按下 > 选中 > 注视 > 常态；「自己写」没有选中态。
- **签贴图**：56 张都能加载，尺寸 0.129 × 0.060 m。
- **布局**：签列和纸条间距 ≥ 2.5 cm；签之间、放飞和重写之间的可按区域都不重叠；签列在纸条高度内居中。
- **线宽**：两种纸上 `SetWidth` 都按比例变细，复位后恢复。

请再补以下测试（可以用现有测试替身，或把判断抽成纯函数）：
1. `PickPreset` → 时间推进到 `Total` 之后，`HasInk` 为真，`strokes` 和预设逐点相等，`PresetWriting` 为假。
2. 预设书写中调用 `ReleaseWish()`：先写完，再进入保存，记录的 `preset` 等于 id。
3. 预设书写中有手指捏合靠近纸面：不产生手写笔划。
4. 预设写完后点「自己写」：笔划清空，线宽回到 1；手写纸上点「自己写」，笔划保留。

跑全量 EditMode，和 wish-ink-visibility-1031 的基线对比（730 项，29 项已有失败），不应新增失败。

> 我这边没有 Unity，也没有 C# 编译器，这些代码**没有编译过**。另做了一轮独立代码检查：逐个核对了引用的工程接口和 C# 9 语法，没发现编译问题；查出的 4 个逻辑小问题已修。补丁已确认能在 8de6c1b 上干净应用；测试里和数据有关的断言，都用 Python 按同样的规则对 json 核对过。编译如果还报错，请直接修，并在验证记录里说明。

---

## 6. 真机验收

1. **签好不好按**：用食指点每一个签，不会按到相邻的签。写字的手扫过左侧时，签不会被误触（`PressBlocked`）。
2. **书写动画**：四个世界各选一句，笔尖光点跟得上笔，没有闪烁、跳帧，写完响一声，放飞按钮跳两下。
3. **方向**：字不是镜像，不是上下颠倒。中文从上往下，英文从左往右。
4. **清晰度**：在写字距离（约 40 cm）和挂起后（0.8–1.6 m）看，「喜」「康」「健」「散」的笔画都分得开。太细就把 `InkWidth` 调到 0.8；太糊就把 `GlowWidth` 调到 0.4。
5. **切换**：书写中换另一句、点重写、点放飞、点「自己写」，都没有残留的笔迹或卡住的光点。
6. **存档**：放飞几个预设风铃，退出再进入世界，挂着的风铃笔迹和写时一样细。混有旧手写风铃也都正常。
7. **英文**：系统语言切到英文，签、纸上的字、按钮都是英文。

---

## 7. 可选：引导语

现在 B4 只教手写。如果想让用户知道可以选一句，可以加一句新旁白，用治愈博主的声音录制，不在本包范围内。参考文案：
- 中文：「也可以在左边挑一句心愿，它会自己写在纸条上。」
- English: "Or pick a wish on the left, and it will write itself."

---

## 交付

在 `docs/verification/` 里写一份记录，内容包括：
- 测试数量和结果；
- 编辑器里四个世界选中一句、写到一半的截图；
- 写完后的截图；
- 编译时修过的问题。

先不要推送，等用户确认。
