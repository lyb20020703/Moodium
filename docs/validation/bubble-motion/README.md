# 泡泡点云 GPU 运动交付与验证

2026-10-30 · Unity 6000.0.30f1 / PolySpatial 2.4.3 · Apple M3 Pro

用户已明确授权暂不等待真机确认，先完成引擎内实现。动态 Graph、双手驱动、四套世界重烘焙和引擎 GPU 验证已完成。设备渲染、真实手部追踪、舒适度和帧率尚未验证。

## 已实现

- 自然世界：空间噪声风、远近幅度、树叶权重、每点独立颤动。
- 糖果世界：上下漂浮、横向摆动、独立节奏。
- 双手：张手触发、捏合冷却、位置平滑、近处球形扰动、沿视线到掌心方向的远处扰动、局部透明度降低和收手回位。
- 阶段：小世界风强度 0.3；进入后 1.5 秒升至 1；外层交接保留当前风、手强度在最多 0.3 秒归零；离开时风在 0.5 秒归零。
- 每个世界使用独立材质；保留原有 `_BaseColor.a` 淡出、呼吸缩放、内外层交接及返回行为。
- 删除旧 `BubbleWorldAmbientWind`、`BubbleWorldHandRipple`、旧风测试和未调用的 `UpdateQuietLayerDrift`。

## 对原方案的必要细化

1. 四角使用相同点中心计算位移，避免原角点公式在手影响边缘拉长方片。中心编码在 Float32 顶点颜色 RGB，`center = (RGB - 0.5) * 256`。显示颜色仍从原贴图读取。PolySpatial 仅支持 UV0/UV1，未使用不受支持的 UV2。
2. UV1 保存相同的 seed/sway；seed 范围严格为 `[0,1)`。旧方片旋转仍使用原哈希，保证重烘焙不改变几何。
3. 两手、风、自身运动和 Boost 合成后，再将总位移限制到 0.8 m；包围盒每边扩展 0.8 m，诊断倍数也不能越界。
4. 所有归一化都带零长度保护。材质更新阈值不会跳过最终归零。
5. C# 噪声参考对应 Shader Graph 的 LegacyMod 算法；PolySpatial 会将 Gradient Noise 换成 MaterialX 原生 Perlin，因此设备上的局部风相位可能不同，不宣称跨平台逐点数值一致。

新增中心和 UV1 数据约增加每顶点 24 字节，即每个 50 万点世界约 48 MB 原始顶点通道数据；同时移除了旧方案的运行时网格复制和蒙皮。实际设备内存、包体及帧率仍需测量。

## 验证证据

### 编译与转换

- Unity 导入及脚本编译成功。
- Graph 和两个 Sub Graph 的所有计算节点均通过 PolySpatial 支持检查，无该 Graph 的不支持节点通知。
- 检查常量实际输入槽值，防止仅字段有值、槽位仍为零。
- MaterialX 输出包含 `GeometryModification`，且与最终材质的 `vertexshader` 相连；全部节点引用可解析。
- 未生成新的 visionOS/Xcode 构建，以上是引擎和本地转换检查。

### 烘焙

四套世界共 80 层、200 万个点，全部完成重烘焙。前后 SHA256 检查确认位置、UV0、三角形索引均未改变，所有资源 GUID 保持一致；逐顶点验证四角中心/UV1、权重和包围盒扩展。

### EditMode

最终结果 **47/47 通过**，见本目录 [editmode-results.xml](editmode-results.xml)。覆盖资源参数契约、独立材质透明度、转换连线、80 层资产、随机种子范围、零输入、远近风幅度、双手与 Boost 上限、无 NaN、方片刚性、手部球形/锥形边界、旋转缩放、触发延迟、张手阈值、捏合冷却、追踪丢失、升降时间常数、位置平滑、交接和清理，以及原泡泡世界与嵌套进入测试。

扩大检查时发现一个既有失败：`BubbleRingTests.FeedbackUsesOneDrawMeshAndReleasesGeneratedAssets` 期待 1 个泡泡环，但当前 `BubbleRingController` 已禁用该入口并返回 0。本次未修改这两个文件，未为通过旧测试而重新开启入口。它不在最终点云运动回归集合内。

### 实际 GPU 渲染

通过 URP `SubmitRenderRequest` 在 Apple M3 Pro 上渲染，非 CPU 示意图。详细指标在 [results.txt](results.txt)，均为 0–1 范围内平均 RGB 像素差，**不是帧率或主观质量评分**。

| 检查 | 结果 |
| --- | --- |
| 新旧材质零运动静态对照 | 平均 RGB 差异 0 |
| Boost=8 自然风 | 明确改变点的位置 |
| 同参数间隔至少 1.2 秒 | 明确改变画面，确认时间驱动有效 |
| 手掌在 0.6 m、点在约 6 m | 远射线扰动可见 |
| 手掌在约 6 m 点云内 | 近球体扰动可见 |
| 四套实际世界，Boost=1 | 均有正常幅度画面变化 |

捕获图只展示点云，不包含完整应用的全景天空、UI 或进入动画。可见方片轮廓延续原有材质/点大小，不是新增的几何形变。

静态对照：[原材质](01-legacy-static.png)、[新材质](02-motion-static.png)。

动态与手部：[风 Boost=8](03-wind-boost8.png)、[稍后同参数](03b-wind-later.png)、[糖果 Boost=8](04-candy-boost8.png)、[远射线](05-hand-ray.png)、[权重诊断](06-hand-debug.png)、[手附近](07-hand-near.png)。

四套实际世界：[自然](nature-normal-motion.png)、[糖果](candy-normal-motion.png)、[自然第二层](nested-nature-normal-motion.png)、[糖果第二层](nested-candy-normal-motion.png)。对应 `*-static.png` 与 `*-hand-debug.png` 可用于对照。

## 在 Unity 中直接预览

打开 **Moodium → Bubble World → Open GPU Motion Sandbox**。

窗口使用隔离的预览场景和材质副本，可切换四个世界、视角、风强度、Boost、模拟手部位置/强度及诊断色。不会修改当前场景或 Resources 材质。窗口刷新上限 10 次/秒，用于观察，不用于性能测量；真实手部触发逻辑由运行时驱动负责。

其他菜单：

- **Build GPU Motion Graphs**：重新生成全部 Graph，保持资源 GUID。生成器使用本机 Shader Graph 17 的内部编辑器 API；升级 Unity 后需重新检查接口。
- **Render Motion Validation Images**：重跑实际 GPU 图像验证，更新本目录 PNG 和结果。
- **Bake Meshes**：重烘焙全部世界；命令行 `BubbleSplatBakeValidation.BakeAndValidate` 还会比较旧几何指纹并验证全部资产。

## 当前默认参数

| 参数 | 值 |
| --- | --- |
| WindStrength / FloatStrength / SelfAmount | 1 / 0.8 / 1 |
| Boost / DebugTint / StaticValidation | 1 / 关闭 / 关闭 |
| 风进入 / 离开 / 小世界强度 | 1.5 s / 0.5 s / 0.3 |
| 张手 / 捏合距离 | >0.06 m / ≤0.035 m |
| 前方距离 / 捏合冷却 / 触发延迟 | >0.25 m / 0.8 s / 0.15 s |
| 手强度升起 / 普通收手时间常数 | 0.35 s / 1 s |
| 外层交接手部清零时长上限 | 0.3 s |
| 位置平滑 / 追踪丢失宽限 | 0.08 s / 0.3 s |
| 半径 / 增量 / 增长 / 回落时间常数 | 0.25 m / 0.10 m / 3 s / 2 s |
| 远射线内角 / 外角 | 4° / 9° |

运行时根物体上的 `BubbleSplatMotion` Inspector 可调试上述参数。资源材质的初始风/手强度为零，运行时按阶段驱动；这不代表运动被禁用。

## 真机待确认

- Xcode 实际 shader 日志、MaterialX 显示、颜色/柔边/排序。
- 四套世界近远点的运动可见性、舒适度，以及正常幅度各 20 秒录屏。
- 实际 XR Hands 张手、捏合、跟踪丢失、双手同时扰动与回家交互。
- 静止/双手帧率、GPU/CPU 时间、内存和改动前同设备对照。

尚无上述实测数据，不将编辑器结果当成真机验收。
