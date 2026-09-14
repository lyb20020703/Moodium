# Moodium 现实增强模式双 Tracking 设计

日期：2026-09-14

## 目标

现实增强模式同时启用 Object Tracking 与 Image Tracking。餐巾纸和测试图片可以同时被识别，各自生成并持续跟随一个 Chocolate Capsule。两个 Capsule 都能独立响应手掌碰撞，播放相同的破裂动画、音效和粒子反馈，并共同增加同一条 Candy Energy 进度。

首个 Image Tracking 测试目标为 `Assets/Moodium/TestImageTracking.png`，实际尺寸按 `0.10 m × 0.10 m` 配置。

## 非目标

- 不区分轻触、按压和拍打速度。
- 不支持用户在运行时导入新的参考图片。
- 不为 Image Tracking 创建另一套动画、粒子、音效或进度系统。
- 不改变空间创造模式的交互。

## 架构

系统分为四层：Tracking Provider、Capsule Spawner、Reality Session Coordinator 和多目标交互。

### Tracking Provider

XR Origin 同时包含并启用：

- 现有 `ARTrackedObjectManager`，继续使用 Tissue Reference Object Library。
- 新增 `ARTrackedImageManager`，使用新的 Moodium Image Reference Library。

Image Reference Library 包含一项：

- 名称：`TestImageTracking`
- Texture：`Assets/Moodium/TestImageTracking.png`
- Specify Size：启用
- Physical Size：`0.10 m × 0.10 m`

两个 Manager 只在现实增强模式中启用，退出该模式时同时关闭。

### Capsule Spawner

保留 `TissueObjectTrackingSpawner` 负责 Object Tracking，并新增 `ImageTrackingCapsuleSpawner` 负责 Image Tracking。两者遵循相同生命周期：

1. 通过 `trackableId` 保证一个追踪目标只对应一个 Capsule。
2. 使用 `Assets/prefab/Chocolate_Capsule.prefab` 生成实例。
3. 禁用移动和缩放编辑，启用 `ChocolateCapsuleInteraction`，关闭 Spatial Pointer 触发。
4. 使用追踪锚点的世界 Pose 更新 Capsule。
5. `Tracking` 或 `Limited` 时显示；`None` 时隐藏；目标被移除时销毁。
6. 生成或恢复可用状态时，将 Capsule 报告给 Reality Session Coordinator。

共享的 Capsule 配置步骤提取为小型公共构建方法，避免 Object/Image 两个 Spawner 的动画和 Collider 配置逐渐分叉。Spawner 仍各自处理 ARFoundation 不同的事件类型和字典。

### Reality Session Coordinator

`RealityEnhancementFlowController` 从单目标状态改为会话级协调器：

- 现实增强模式启动时同时订阅 Object/Image 检测事件。
- 初始引导仍只显示一次。
- 每个新目标拥有独立的生成协程和出现动画。
- Capsule 出现动画完成后注册到多目标手掌交互控制器。
- 一个目标丢失或移除只注销自身，不停止另一种 Tracking 或其他 Capsule。
- 退出模式时取消所有生成协程、注销全部 Capsule，并要求两个 Spawner 清理运行时实例。

全局状态仅表示 Reality Session 是否运行；不再用一个 `m_TrackedObject` 或一个 `m_Target` 表示整个场景。

### 多目标手掌交互

`PalmPressGestureController` 改为维护多个 `ChocolateCapsuleInteraction` Target：

- `RegisterTarget(capsule)` 注册一个可交互 Capsule。
- `UnregisterTarget(capsule)` 清理该 Capsule 的接触状态。
- 每次 XR Hands Dynamic 更新仅依赖左右手各自的 Palm Pose。
- Palm 碰撞盒通过 Physics overlap 找到命中的 Capsule Collider。
- 接触状态以“手 + Capsule”为单位保存；首次进入调用对应 Capsule 的 `PalmContactStarted()`，离开调用 `PalmContactEnded()`。
- 同一只手从 Capsule A 移到 Capsule B 时，A 正常释放，B 独立触发。
- 左右手可以同时触发不同 Capsule。

不加入速度、力度或停留时间阈值。现有冷却只阻止同一接触抖动造成短时间重复触发。

## 进度与反馈链

所有 Capsule 继续使用静态事件 `ChocolateCapsuleInteraction.AnyCapsulePinched`。`CandyWorldManager` 只订阅一次，并接受以下任一合法来源：

- Capsule 绑定到 `ARTrackedObject` 锚点；或
- Capsule 绑定到 `ARTrackedImage` 锚点。

每次合法触发按现有规则执行：

1. 播放 `ChocolateCrack` 动画与 Capsule 粒子。
2. 播放现实增强模式挤压音效。
3. 以被触发 Capsule 的 Burst Center 生成糖果喷出。
4. 调用 `CandyEnergyController.RegisterPinch()`。
5. 同步更新腕部 HUD 和世界进度控制器。

Object 与 Image Capsule 共用能量总量，不分别维护进度。

## 跟随与坐标

两类追踪结果都由 ARFoundation 提供世界锚点 Transform。Capsule 使用通用的 Anchor Pose Follower，在 `LateUpdate` 中显式写入世界位置和旋转，避免依赖父子层级变化被 PolySpatial 间接同步。

Image Capsule 默认与图片中心重合。其局部旋转和偏移保留为可序列化配置，以便真机测试时调整模型是在图片表面、上方还是面向用户。初始值为零偏移和单位旋转。

手掌关节 Pose 继续通过当前 `XROrigin` 转换为 Unity 世界空间，再与 Capsule Collider 比较。

## 丢失、恢复与冲突处理

- `Tracking`、`Limited`：保留 Capsule 并继续使用最新可用 Pose。
- `None`：隐藏 Capsule、结束其当前接触，但保留实例等待恢复。
- Removed：注销并销毁对应 Capsule。
- 同一张参考图片和餐巾纸同时存在：分别生成 Capsule，不做优先级竞争。
- 多个相同图片实例：若 Provider 返回不同 `trackableId`，分别生成 Capsule。
- 一个 Capsule 被销毁时，交互注册表会清除空引用，避免残留接触状态。

## 场景与资源改动

- 新建 XR Reference Image Library asset。
- 将 `TestImageTracking.png` 以 10 cm 方形参考图加入 Library。
- 在现有 XR Origin 上添加并配置 `ARTrackedImageManager`。
- `MoodiumAppFlowController` 增加 Image Manager 和 Image Spawner 引用，并在现实增强模式切换时与 Object Manager 同步启停。
- 保留当前 Object Tracking 资源与 Tissue 配置。

## 测试

EditMode 回归测试覆盖：

1. Object 与 Image Spawner 使用相同 Capsule 运行时配置。
2. 同一个 `trackableId` 不会重复生成 Capsule。
3. 两个不同来源可同时注册并保持两个 Capsule。
4. Palm 命中 Capsule A 只触发 A，命中 B 只触发 B。
5. 左右手接触状态互不覆盖。
6. Image Anchor 移动后 Capsule 世界 Pose 更新。
7. Image Capsule 触发后同一个 Candy Energy 增长。
8. Image 进入 `None` 或 Removed 后正确隐藏、注销与清理。
9. 退出现实增强模式后 Object/Image Managers 和全部运行时 Capsule 被清理。

真机验收覆盖：

1. 10 cm 打印图片可被稳定识别。
2. 移动和旋转图片时 Capsule 跟随。
3. 餐巾纸与图片同时出现两个 Capsule。
4. 分别触碰两个 Capsule 均播放破裂反馈并增加同一进度条。
5. 遮挡、移出视野、重新出现后不会生成重复实例。

## 成功标准

- Object Tracking 与 Image Tracking 在现实增强模式中同时运行。
- 两个来源同时存在时各自显示一个可跟随 Capsule。
- 两个 Capsule 可独立触发完整反馈链。
- 两类触发共同更新同一进度。
- Object Tracking 原有行为、空间创造模式及世界选择界面不回归。
