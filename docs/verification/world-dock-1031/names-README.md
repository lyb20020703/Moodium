# 悬浮按钮改名 · 补充包（2026-10-31）

`Moodium-WorldDock-1031` 的补充。**实现的 agent 请先读 `AGENT_TASK.md`。**

「从天而降」改为按世界取名，直接说出会落下来的东西：

- 糖果世界：**糖果雨** / Candy Shower
- 自然世界：**花草雨** / Meadow Shower（自然世界掉的是花、四叶草、蘑菇）
- 下雨中：**雨停啦** / Stop
- 未解锁小字：能量集满就能下
- 能量满提示：能量满啦，下一场糖果雨吧（自然世界是花草雨）

![按钮状态](Preview/states.png)

演示视频：
- `Preview/demo_candy_zh.mp4`
- `Preview/demo_nature_zh.mp4`
- `Preview/demo_nature_en.mp4`

## 目录

| 路径 | 内容 |
|---|---|
| `AGENT_TASK.md` | 替换哪些文件、删哪 4 个旧贴图、测试 |
| `Unity/Resources/MoodiumWorldDock/` | 全套 47 张贴图（整体替换） |
| `Unity/Scripts/WorldActionDock.cs`、`Unity/Editor/WorldActionDockTests.cs` | 替换版 |
| `Patch/*.diff` | 和上一版代码的差异 |
| `Preview/` | 状态图、效果图、演示视频 |
| `Source/` | 改过文案的导出脚本和预览页 |

## 状态

- 文案、贴图、代码、测试和预览都已完成。
- 项目代码没有改。新代码没有在 Unity 里编译过，改动只有贴图名和一个参数。
