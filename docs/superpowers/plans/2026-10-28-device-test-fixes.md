# Device Test Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** 修复设备测试发现的语音、点云、亮度、地面贴图和泡泡入口问题。

**Architecture:** 先改互相独立的渲染和语音基础设施，再把泡泡入口改为 CandyWorldManager 驱动的虚拟捏捏位置召唤。保留当前既有房间暂停、Bubble/Explore 阶段和退出动画。

**Tech Stack:** Unity 6000.0.30f1、PolySpatial 2.4.3、AR Foundation 6.0.3、XR Hands 1.5.0、NUnit EditMode。

**Spec:** docs/superpowers/specs/2026-10-28-device-test-fixes-design.md

## Global Constraints

- 不 commit、reset、revert、clean、stash。
- 现有 EditMode 测试继续运行，Unity 编译无错误。
- 关键日志分别使用 `[Moodi Voice]`、`[Point Clouds]`、`[Prefab Lighting]`、`[Floor Decal]`、`[World Journey]`。
- 保留当前 Image Tracking、Sensory Bag 分支、Bubble/Explore 阶段和无关工作区修改。

## Review Focus

- 语音实例被隐藏时全局队列仍按预计结束时间推进；由 MoodiVoiceTests 覆盖。
- 树采样低接受率仍达到预算且树干比例满足 12%；由 PointCloudFxTests/模型验证覆盖。
- 透明贴图不写深度且糖纸排序稳定；由 FloorDecalTests 与导入设置检查覆盖。
- Bubble 阶段重复进入与手腕退出不会错误恢复或销毁缓存；由 BubbleWorldTests 覆盖。
- 互动结束正在说话时，泡泡入口延迟到语音空闲；由 WorldJourneyFinaleTests 覆盖。

### Task 1: 全局 Moodi 语音仲裁

**Files:** 修改 `Assets/Scripts/Moodium/Characters/MoodiVoice.cs`、`Assets/Editor/MoodiVoiceTests.cs`。

- [ ] 增加静态 `IsAnyoneSpeaking`、`s_BusyUntil`、最多 3 条的必说 FIFO 队列、保留 ID 集合和超时规则。
- [ ] `Say`/`SayAfter` 按必说/可省分类排队，`Stop` 清空全局队列；`Play` 不再主动停止前句；Emitter 脱离 Moodi 层级。
- [ ] 为隐藏实例、队列满、可省丢弃、K10→A2 时序添加测试。
- [ ] 运行 `MoodiVoiceTests`。

### Task 2: 树点云密度与图集

**Files:** 修改 `Assets/Editor/PointCloudModelBaker.cs`、`Assets/Scripts/Moodium/PointClouds/PointCloudFx.cs`、`Assets/Editor/PointCloudFxTests.cs`、相关资源 `.meta`。

- [ ] 记录并输出接受率、实际/目标点数、树干/叶片点数和运行时世界空间点大小。
- [ ] 树配置使用 140000 点、1.4 点大小系数、12% 树干/枝条下限、60 倍最大采样尝试。
- [ ] sharedGlow 图集布局支持 140000 点使用 2048 颜色图，并更新平台导入设置：Uncompressed、无 Mipmap、Bilinear、Max Size 4096。
- [ ] 烘焙 Birch/Oak 并记录实际点数；增加低接受率、预算、图集和树干比例测试。

### Task 3: RealityKit 预制体亮度

**Files:** 新增 `Assets/Scripts/Moodium/Rendering/PrefabStudioLighting.cs`；修改预制体材质、必要的编辑器验证和说明文档。

- [ ] 扫描五个预制体记录 shader、Base Color、Metallic、Smoothness、Emission 和 Surface Type。
- [ ] 对非金属材质应用 Metallic=0、Smoothness 0.3–0.5、Emission 0.15–0.3；果冻保持高 Smoothness并提供日志。
- [ ] 对运行时实例添加亮度兜底组件，找不到环境光源时只警告不阻塞。
- [ ] 增加材质参数 EditMode 验证。

### Task 4: 糖果世界地面贴图

**Files:** 修改 `WalkTrailGuideController.cs`；新增 `FloorDecalRenderer.cs`、`FloorDecalAssetPostprocessor.cs`、测试和 `walk_growth_glow.png`。

- [ ] 使用共享透明 Unlit 材质和 MeshRenderer，Depth Write 关闭、双面、无阴影，alpha 修改材质实例。
- [ ] 预加载导入 PNG，地面内容离地 0.025m；固定糖纸 order 0、地面内容 order 1。
- [ ] 设置 Alpha Is Transparency、无 Mipmap、带 alpha 压缩或不压缩，并增加导入设置检查。
- [ ] 增加黑底/透明排序/资源预加载测试。

### Task 5: 虚拟捏捏上方泡泡入口

**Files:** 修改 `BubbleRingController.cs`、`BubbleWorldController.cs`、`CandyWorldManager.cs`、相关测试和验证文档。

- [ ] 停止生成地面/桌面光环；保留清理接口但不再调用。
- [ ] 在 Landed→Free+1.5s、A2 后20s或无救援故事满能量后4s三种条件下，且语音空闲时调用 `Summon`。
- [ ] 召唤点使用 `Popped` 果冻罩中心上方0.3m并朝用户0.05m；回退到校准袋中心或前方0.6m。
- [ ] 泡泡从捏捏顶面升起，常驻不自动消失；返回房间后回到原位并保持，手腕退出销毁。
- [ ] F1/F1t 从注册表移除，覆盖 F7 中英文语音资源，更新 F2/F7 测试和入口时序测试。

### Task 6: 全量验证与说明

- [ ] 运行相关 EditMode 与 Unity 编译，记录通过/失败/跳过。
- [ ] 检查 `git diff --check`，记录生成 Prefab 的既有空白差异。
- [ ] 更新 `docs/verification/2026-10-28-device-test-fixes.md`，列出实际点数、材质记录、闪烁原因、Inspector 参数和真机待验。
