# 泡泡世界第二层：重新导出，点和第一层一样细（2026-10-30）

基准：`codex/realdevice-fixes-1030b` 的 `f47713a`。第一层没有任何改动。
- 数据部分已在本机导出完成，并通过了原有的配准校验。
- Unity 部分（重新烘焙网格、跑测试）还没做。

## 原因
第二层是用 `tools/convert_gaussian_to_bubble_world.py` 导出的。它算点大小的规则和第一层（`resample_nature_cloud.py`）不同：
- **第一层：** 高斯尺度 × 1.68，夹在上下限内，再整体 × 0.632（上下限也一起缩）。
- **第二层：** 高斯尺度 × 1.68 × √(原始点数 / 50 万)。原始扫描有 85–95 万个点，所以是 × 2.2–2.3；上下限没有缩。
- 结果是第二层的点在视野里大了约 2 倍，两到三成的点被顶到了允许的最大尺寸。运行时代码没有问题。

## 改法
1. **点大小：** 导出脚本新增 `--footprint layer1`（默认），规则和第一层完全一样。旧规则保留为 `--footprint legacy`。用 `--footprint legacy --sampling capture` 可以逐字节重现旧数据，已验证。
2. **选点：** 新增 `--sampling eye`（默认）。从站立眼高（1.5 m）看出去，把视野分成等面积的方向格，选点时每格权重做平方根均衡，稀疏方向（远处、天空）多分一些名额。
3. 点数仍是 50 万，和其他世界一致。

## 数据对比（从站立眼高看，`evalpc.py`）

| | 点的中位大小 | p90 | 大于 1° 的点 | 地平线以上覆盖 | 仰角 30° 以上覆盖 |
|---|---|---|---|---|---|
| 自然 第一层（参考） | 22.0′ | 36.0′ | 0.3% | 70% | 57% |
| 自然 第二层 旧 | 43.6′ | 67.0′ | 15.8% | 42% | 13% |
| **自然 第二层 新** | **24.0′** | **36.6′** | **0.1%** | 35% | 10% |
| 糖果 第一层（参考） | 24.1′ | 37.9′ | 0.8% | 71% | 51% |
| 糖果 第二层 旧 | 45.7′ | 67.3′ | 17.1% | 85% | 71% |
| **糖果 第二层 新** | **24.2′** | **36.8′** | **0.0%** | 61% | 42% |

对比图在 `preview/`：左边是第一层，中间是旧的第二层，右边是新的第二层。灰色是缝隙，真机上缝隙里看到的是柔和天空色。

## 取舍
- **缝隙变多：** 点变小后，地平线以上的覆盖率下降（自然 42% → 35%，糖果 85% → 61%），缝里会透出柔和天空色。第一层本来就是这样。
- **自然第二层天空稀：** 原始扫描在仰角 30° 以上只有约 1.5 万个点，这是扫描本身的限制。试过：
  - 把点数加到 70 万：覆盖率只多 3%，不值得多花 40% 的顶点；
  - 减少脚下附近的点：没有效果。原因是 79 万个可用点要选 50 万个，大部分点无论权重高低都会被选中。
- 点的位置、配准参数完全没变，所以泡泡入口、地平线、拍摄高度都不受影响。配准校验也通过了：
  - 自然：误差 0.44°，分差 0.26；
  - 糖果：误差 0.13°，分差 0.22。

## 文件
| 文件 | 内容 |
|---|---|
| `tools/convert_gaussian_to_bubble_world.py` | `--footprint`、`--sampling`、`--viewer-eye-height`、`--direction-balance`；frame.json 记录 `export_options` |
| `NestedNature/` 和 `Candy/Nested/` 下的 `points.bytes`、`point_colors_round.png`、`frame.json` | 新数据。frame.json 新增 `export_verification`，并在 `previous_export` 里记录旧哈希 |
| `Assets/Editor/NestedFootprintTests.cs`（新） | 第二层点的中位视角不超过第一层的 1.25 倍；烘焙网格与 points.bytes 一致（防止忘了重新烘焙） |

## 重新导出命令
```
python tools/convert_gaussian_to_bubble_world.py <Nature_Layer2>/b792f7bb25e24fbe792219ac5b03b953.ply <out> --frame-json docs/verification/second-layer-horizon/NestedNature.refined.frame.json --verify-panorama Assets/Resources/MoodiumBubbleWorld/NestedNature/panorama.png
python tools/convert_gaussian_to_bubble_world.py <Candy_Layer2>/b7dbd00c3878b09ecbf9d2f3e1851e7f.ply <out> --frame-json docs/verification/second-layer-horizon/Candy-Nested.frame.json --verify-panorama Assets/Resources/MoodiumBubbleWorld/Candy/Nested/panorama.png
```


## Unity 实施与验证

在原工程从 f47713a 创建 codex/nested-footprint-1030，原样应用数据和导出脚本补丁，提交 e99395e。第一层数据及网格未改。

在独立验证副本运行 Unity 6000.0.30f1 的 BubbleWorldBaker.BakeBothNested，成功烘焙两个第二层，各 500000 点、20 层；自然微缩泡泡使用 14 层，糖果 12 层。生成的 40 个点层网格和两张 round 颜色图已同步回原工程。球面网格未产生内容差异。未重新运行第一层烘焙。

注意：补丁预生成的 round 颜色图和 Unity 烘焙输出不仅 PNG 文件字节不同，解码比较也不一致。本次采用项目现有 BuildRoundAtlas 从新 points.bytes 直接生成、并用于测试的 Unity 输出，没有声称与输入 PNG 完全相同；该颜色差异仍需视觉核验。未改动原 panorama、配准矩阵或运行时朝向代码。

全套 669 项：**640 通过、29 失败**。相对 f47713a XML 逐项比较，新增失败 0、消失失败 0。[完整结果](nested-footprint-1030/full-tests.xml)、[旧失败清单](nested-footprint-1030/remaining-failures.md)、[烘焙日志](nested-footprint-1030/bake.log)。

NestedFootprintTests 4/4，包含两世界的中位视角 ≤ 第一层 1.25 倍及烘焙网格与新数据一致；BubbleWorldTests 30/30、NestedEntry 2/2、Sky 3/3、HandWake 8/8。测试使用实际重新烘焙后的资源。未调整旧测试期望。

验证副本起初包配置缺少依赖，随后同步原工程 Packages；一次 Git 包拉取网络失败，重试恢复后才完成上述烘焙和全套验证。没有修改原工程依赖配置。原工程既有 FBX 改动未纳入本次提交。

真机视觉未验收：第二层点的细度、地平线/入口不变、颜色图一致性和缝隙中的 Soft 天空观感仍待确认；输入包 preview 是离线示意，未当作设备验收截图。当前分支未合并、未推送。
