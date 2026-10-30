# 泡泡点云 GPU 浮动与手部扰动设计

状态：完整引擎实现已接入。用户明确授权暂不等待真机确认，先完成引擎内实施与验证；真机视觉、手部追踪和性能另行验收。依据用户提供的 bubble-splat-motion-agent-prompt.md。

## 实施前基线

- 基线 main / 26bb73d；本地另有开机语言与硬件按钮上移修改，保留。
- SourceFrameRotation 已统一返回 identity；第二层 frame.json 与直接进入的 Portal、HandingOff 已存在。
- BuildLayerMesh 目前仅写 UV0，没有运动权重。四套世界共 80 个点层资源。
- AmbientWind 每层建立 36 根骨骼，复制网格并用 SkinnedMeshRenderer；HandRipple 用掌心速度驱动这些骨骼。
- Controller 仍创建两种旧组件，UpdateQuietLayerDrift 未调用。新实现必须保留 _BaseColor alpha、呼吸缩放、嵌套交接和返回行为。

## 目标和资产

使用 PolySpatial 可转换的 Unlit Shader Graph 内置节点，Object 空间计算顶点位移。CPU 每帧仅更新少量材质参数，不修改顶点、不创建骨骼。

- Assets/Shaders/BubbleSplatMotion.shadergraph：Moodium/BubbleSplatMotion。
- 同目录 HandDisturb.shadersubgraph、SplatHash.shadersubgraph。
- Assets/Resources/MoodiumBubbleWorld/BubbleSplatMotion.mat：直接引用 Graph，避免构建剥离。
- BubbleSplatMotion.cs：双手采样、坐标换算、强度与阶段管理。
- BubbleSplatMotionMath.cs：与节点公式一致的 CPU 数学参考与测试入口。

Graph 使用 Transparent / Alpha、双面、Alpha Clipping（阈值 0）。保留圆点贴图 UV0 和 _BaseMap、_BaseColor。时间使用 PolySpatial Time；具体序列化节点及 MaterialX 转换结果需用本地安装包和 Unity 导入验证，不能仅凭编辑器显示判定真机支持。

## 烘焙

为 BuildLayerMesh 增加世界风格参数，Baker 与运行时 fallback 共用路径。UV1 每个点四角写相同 seed、sway。

- seed 沿用全局索引哈希整数，除数使用 65536，保证 [0,1)；原方片旋转仍使用旧 Hash01，保持几何完全不变。
- h = position.y + EyeHeight。
- 自然 sway 按原文 tree/bush/grass 取最大，底数 0.2；糖果按高度从 0.35 到 1。
- bounds 每侧增加 0.8 m，即 Bounds.Expand 的总尺寸增量为 1.6 m。
- 按四套世界重烘焙 80 层，保留 GUID、点数、UV0、颜色、地平线和空间朝向。
- 额外使用 Float32 顶点颜色编码点中心：RGB = center / 256 + 0.5，四角相同。显示颜色仍来自 UV0 贴图。PolySpatial 2.4.3 只支持 UV0/UV1，不能用 UV2 存中心；Vertex Color 节点可转换成 MaterialX geomcolor。

## 顶点与片元

原文第 4.3 节为公式基准：自然空间噪声风、糖果缓慢漂浮、seed 控制自身浮动，以及两只手的近球体与远射线影响。运动函数的 P 使用解码后的点中心；Vertex Position 输出仍为原角点位置加位移，因此四角运动严格相同。HandDisturb 输出位移与 k；片元以相同中心计算 k，用于 60% 最大局部淡出和调试红色权重显示。整体淡出继续乘 _BaseColor.a。

参数保留原文第 4.2 节全部名称。额外暴露射线内外角对应的余弦参数，保证 Inspector 的 4°/9°设置实际进入 Graph；侧向量可由 C# 计算或使用经转换验证的节点计算。

## 驱动与阶段

每个实例独立持有材质和驱动。世界掌心/眼睛使用 InverseTransformPoint；上方向、风向转为局部方向并归一化。手影响半径还需除以世界缩放，保持 25–35 cm 的实际物理半径。正常 Inside 的呼吸为均匀缩放。

| 阶段 | 风 | 手 |
| --- | --- | --- |
| Floating、Portal、Approach、Envelop、Reveal | 0.3 | 禁用 |
| Inside | 1.5 秒升至 1 | 开启 |
| HandingOff | 保持当前风强度 | 0.3 秒降至 0 |
| Leaving | 0.5 秒降至 0 | 禁用并衰减 |
| Armed、Idle、停用、清理 | 0 | 清零 |

两只手独立保存追踪时间、最近捏合时间、稳定张手时长、平滑位置、amount 与 growth。掌心前方超过 0.25 m，拇食指距离超过 0.06 m，捏合冷却 0.8 秒，持续 0.15 秒后触发。amount 升/降时间常数 0.35/1 秒，位置平滑 0.08 秒，growth 3 秒增长、2 秒回落。失踪超过 0.3 秒视为收手；无关节数据不能判为张手。禁用、重新进入要清状态，避免复用上次手势。

材质参数使用变化阈值减少重复写入，保证归零的最后一次写入不会被阈值吞掉。不用全局 Shader 参数。

## 需要在实现中明确的数学边界

1. 原文 0.8 m 是单手 mag 的上限；两个手位移叠加，再加风、自身浮动和 Boost 后，总位移可能超过 0.8 m。因此最终合成位移也限制为 0.8 m，与固定扩展包围盒及验收一致。Boost=8 的诊断仍有效，但会饱和到安全上限。
2. 原文按角点位置求值不能保证任意方片位移差小于边长 5%，尤其在手影响边缘。最终采用额外点中心数据：四角参数完全相同，零差异位移避免撕拉，片元手部透明度也在同一方片内一致。
3. CPU Gradient Noise 实现匹配 Unity Shader Graph 的 LegacyMod 算法。PolySpatial 的 GradientNoiseAdapter 会转成 MaterialX 原生 Perlin；因此跨平台噪声相位不保证逐点一致。整体公式、范围和时间频率相同，不使用 Mathf.PerlinNoise 冒充等价实现。
4. 远射线使用眼睛到掌心的方向，原文称“手指方向”，实际并不采样食指朝向；按原公式实现。

## 分阶段验收

用户已解除“每阶段先真机确认”的实施门槛。引擎内完成静态对照、Boost=8、正常幅度四世界渲染、手部近球体与远射线响应，以及数学/驱动/烘焙测试。旧组件与未使用漂移已删除。

真机仍需确认 MaterialX 的实际显示、动态时序、真实追踪与帧率；Editor GPU 渲染和转换结果不能代替这些结论。完整记录见 `docs/validation/bubble-motion/README.md`。

## 参考

- 用户提供文档第 3–9 节为公式、默认值与验收基准。
- https://github.com/xibihe/walk-into-memory/blob/main/app/Vendor/MetalSplatter/MetalSplatter/Resources/SplatProcessing.metal
- https://github.com/xibihe/walk-into-memory/blob/main/app/WalkIntoMemory/ArtworkRenderer.swift
