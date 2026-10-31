# 心愿风铃打磨 · 交接包（2026-10-31）

基于 `codex/bubble-play-1031`（1af429c）。**给实现的 agent：先读 `AGENT_TASK.md`。**

## 两件事

1. **重写 / 放飞按钮换成液态玻璃**
   - 和开机按钮同一套配方：透明玻璃胶囊、边缘高光、顶部反光、柔和阴影。
   - 图标和文字烘进贴图。放飞带世界主题色（糖果粉、自然嫩绿），没写字时变淡、按不动。
   - 按钮离纸条更远一点，正在写字时按不到。
2. **跟随泡泡不再和风铃撞在一起**
   - 风铃周围 35 cm 是禁区，泡泡不进。
   - 伸手去写或正在写字时，泡泡滑到不写字那只手那侧，缩小、变淡、固定不动、上锁：碰一下、注视加捏合都不会触发。
   - 真想走，手指按住泡泡 0.6 秒，光环填满就进入或离开。
   - 放飞完成后泡泡自动回原位。

![按钮在四个世界里的效果](Preview/buttons_in_worlds.png)
![泡泡让位示意](Preview/bubble_yield_layout.png)

## 目录

| 路径 | 内容 |
|---|---|
| `AGENT_TASK.md` | 实现说明：改哪些文件、具体代码、数值、测试、真机验收 |
| `Unity/Resources/MoodiumBubblePlay/WishButtons/` | 22 张按钮贴图（3 倍导出，透明底） |
| `Unity/Editor/WishButtonAssetPostprocessor.cs` | 贴图导入设置（按钮本体 9 × 3.1 cm） |
| `Unity/Scripts/BubbleWorld/Play/WishActionButton.cs` | 玻璃按钮组件（换贴图、禁用、防误按） |
| `Unity/Scripts/BubbleWorld/Play/ChimeFocus.cs` | 让位、禁区、停靠点、按住离开的规则和数值 |
| `Unity/Editor/WishChimePolishTests.cs` | 对应的 EditMode 测试 |
| `Preview/` | 两张预览图 |
| `Source/` | 重新导出贴图和预览图的脚本 |

## 状态

- 贴图、代码骨架、测试、实现说明已完成。
- 项目代码没有改，由其他 agent 接入。我这边没有在 Unity 里编译过这几个文件，需要接入时一并编译、跑测试。
- 真机上的按钮清晰度，以及停靠位置用起来顺不顺手，需要上真机确认。
