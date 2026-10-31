# 悬浮按钮文案补充包：「从天而降」改名为「糖果雨 / 花草雨」

这是 `Moodium-WorldDock-1031`（悬浮按钮包）的补充，只改文案和对应的贴图：
- 如果悬浮按钮包**还没接入**，先按它的 `AGENT_TASK.md` 接入，再做本包；
- 如果**已经接入**，直接做本包。

## 改了什么

按钮按世界取名，名字说的就是会落下来的东西。自然世界掉落的预制体是 `NatureBurstFlower`、`NatureBurstClover`、`NatureBurstMushroom`，所以叫「花草雨」。

| | 原来 | 现在（中文） | 现在（English） |
|---|---|---|---|
| 糖果世界按钮 | 从天而降 / Sky Shower | **糖果雨** | **Candy Shower** |
| 自然世界按钮 | 从天而降 / Sky Shower | **花草雨** | **Meadow Shower** |
| 下雨中（再点停下） | 停止降落 / Stop Shower | **雨停啦** | **Stop** |
| 未解锁小字 | 能量集满后开启 | **能量集满就能下** | Opens at full energy（不变） |
| 能量满提示 | 能量满啦，试试「从天而降」 | **能量满啦，下一场糖果雨吧** / **…花草雨吧** | **Energy full! Try Candy Shower** / **…Meadow Shower** |
| 未解锁时点击的提示 | 不变 | 继续捏一捏，集满能量就能用 | Squeeze more to unlock it |

另外，按钮上超过 11 个字符的英文标签（Meadow Shower、Confirm Exit）字号从 24 降到 21，避免图标贴到胶囊边缘。

## 步骤

1. **贴图**：用本包 `Unity/Resources/MoodiumWorldDock/` 的 47 张 PNG **整体替换**工程里的 `Assets/Resources/MoodiumWorldDock/`。
   - 删掉这 4 个旧文件及其 `.meta`：
     - `dock_shower_locked_zh.png`
     - `dock_shower_locked_en.png`
     - `dock_toast_ready_zh.png`
     - `dock_toast_ready_en.png`
   - 新增 8 个：
     - `dock_shower_locked_{candy|nature}_{zh|en}.png`
     - `dock_toast_ready_{candy|nature}_{zh|en}.png`
   - 其余同名文件只是文字变了，直接覆盖。
   - 对文件夹点一次 Reimport。导入设置不用改，沿用 `WorldDockAssetPostprocessor`。
2. **代码**：用本包的文件替换工程里的同名文件。
   - `Unity/Scripts/WorldActionDock.cs` → `Assets/Scripts/Moodium/WorldActionDock.cs`
   - `Unity/Editor/WorldActionDockTests.cs` → `Assets/Editor/WorldActionDockTests.cs`
   - 和上一版的差异见 `Patch/*.diff`：
     - 未解锁贴图名加了世界（`dock_shower_locked_{candy|nature}_{lang}`）；
     - `ToastSprite` 多一个 `nature` 参数，"能量满"提示按世界区分；
     - 注释和日志里的 "Sky Shower" 改成 "Shower"。
   - `CandyWorldManager` 和两个补丁都不受影响。
3. **测试**：
   - `WorldActionDockTests` 里按钮贴图总数从 38 变成 40；
   - 新增 `WorldDockNamingTests`，确认 4 个旧文件已删除；
   - 跑全量 EditMode，不应新增失败。

> 没有在 Unity 里编译过。改动只有字符串拼接和一个方法参数，风险很小。

## 真机看一眼

- 两个世界、中英文各看一次：未解锁、可用、下雨中三种状态的文字都在胶囊内，清楚、不贴边。
- 能量满时的提示说的是当前世界的名字（糖果雨 / 花草雨）。

先不要推送，等用户确认。
