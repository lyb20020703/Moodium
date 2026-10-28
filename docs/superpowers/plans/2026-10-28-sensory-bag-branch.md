# Sensory Bag Branch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 按有／无感官袋拆分选关和校准，共用既有世界生成路径。

**Architecture:** 保留贴纸控制器，新增平行轮播控制器；AppFlow 统一调度所有权分支，Coordinator 统一默认确认与交互校准。TablePlaneSelector 是纯函数，TableAnchorFinder 只适配现有 AR 会话。

**Tech Stack:** Unity 6000.0.30f1、PolySpatial 2.4.3、AR Foundation 6.0.3、XR Hands 1.5.0、TextMeshPro、NUnit EditMode。

**Spec:** docs/superpowers/specs/2026-10-28-sensory-bag-branch-design.md

## Global Constraints

- 状态：待用户确认设计；未开始产品代码实施。
- 不 commit、reset、revert、clean、stash，不覆盖与本任务无关的工作区改动。
- 不新增 ARSession；无袋整条路径不启用图像追踪。
- 不改校准数学、框 UI、心情表盘、世界反馈／能量与 XR Hands 主系统。
- 复用资源、双语文案、字体和非等比缩放补偿；控件不挂在轮播拖动区下面。
- 延续当前按需签到／直接返回；不恢复自动后测或开机自动签到。

## Review Focus

- SetExistingModules、签到恢复、返回入口不得意外重新启用无袋图像追踪：Task 5 integration tests。
- 默认确认不得读前次校准或发两次完成事件：Task 2 idempotence + field parity tests。
- 找桌面后合成锚点在确认后仍被 presentation 使用：Task 3/5 anchor lifecycle tests。
- gaze、鼠标、手指争用、tracking lost、隐藏过程中必须释放捕获：Task 4 input cancellation tests。
- 不同资源实际 Prefab 的非等比缩放、滑块和触碰目标不能互相遮挡：Task 4/6 prefab + visual checks。

---

### Task 1: 基线、所有权与双语文案

**Files:** 新增 Flow/SensoryBagOwnership.cs；修改 Opening/OpeningVideoHardwareChoice.cs、Opening/OpeningManager.cs、MoodCheck/MoodCheckCopy.cs；新增 Assets/Editor/SensoryBagOwnershipTests.cs。

**Interfaces:** `SensoryBagOwnership.HasBag`、`SetHasBag(bool)`、Key 常量；原开机视频逻辑不改变，仅持久化回答。

- [ ] 记录 git status 和现有相关／全部 EditMode 基线结果，不写入或覆盖其他功能资源。
- [ ] 写并运行失败测试：未存默认 true，写 false／true，旧用户 Skip 不覆盖值，已选择后 Skip 保留新值。
- [ ] 实现 PlayerPrefs Key="Moodium.HasSensoryBag"，0／1，Save，开机按钮实际选择时调用；分支日志一次。
- [ ] 增加四个指定中英文 key；测试两个语言均非空、文案符合原文。
- [ ] 复测并记录结果，不提交。

### Task 2: 共用确认路径与无贴纸校准入口

**Files:** 修改 Calibration/SensoryBagCalibrationCoordinator.cs、SensoryBagCalibrator.cs、SensoryBagCalibrationHandle.cs；新增／扩展 Coordinator 和 DefaultConfirmation 测试。

**Interfaces:** `ConfirmDefault(StickerWorldEntry entry)`；`BeginAnchoredCalibration(StickerWorldEntry entry, Transform anchor)`；`SensoryBagCalibrator.HasUserInteracted`。

- [ ] 写失败测试：ConfirmDefault 不创建 calibrator／视频，完成一次；默认数据与 Begin→Confirm 逐字段一致；历史修改结果被忽略；空 ReferenceImageName 可校准，WorldKind 来自 world。
- [ ] 抽出共享 `CreateInitialData`／`CompleteCalibration`，保持 settings .40/.25/.05、CenterOffset=0、PlaneLocalOffset.y=-.05、LocalOrientation=Euler(90,0,0)。
- [ ] 加入入场请求完成去重；以相同 CreatePresentation 生成 Soft。
- [ ] 添加只读用户交互标记：Begin 重置，拖动／升降／Reset／Confirm 标记，不改框数学与视觉。
- [ ] 测试底座在 anchor.y-.05、任意旋转与非等比尺寸仍沿用原 presentation。
- [ ] 跑全部校准相关 EditMode；记录缺失可选视频的一次警告不阻塞。

### Task 3: 桌面纯规则与运行时检测适配

**Files:** 新增 Calibration/TablePlaneSelector.cs、TableAnchorFinder.cs；新增 Assets/Editor/TablePlaneSelectorTests.cs、TableAnchorFinderTests.cs。

**Interfaces:** Candidate(世界 pose、alignment、classification、boundary)；`TrySelect(candidates, viewerPose, settings, out landing)`；fallback/anchor pose 纯函数；Finder 一次 AnchorReady 事件、StopDetection 与 ClearOwnedAnchor 分离。

- [ ] 写失败测试：Table 优先、Floor 拒绝、upward、±40°、水平1.5m、眼下.2–1m，最近目标点选择。
- [ ] 写边界测试：内部点不动，外部点夹最近边，不规则多边形边角、空边界用 extents；后备前方.5m桌面眼下.45m，anchor再高.05m。
- [ ] 实现纯规则和 settings，避免要求真实 XR 才能测试。
- [ ] Finder 复用现有 XROrigin 的 ARPlaneManager，无则加，Horizontal、planePrefab=null，默认 off；查找时显示 calib.searching。
- [ ] 无 subsystem／权限立即 fallback + 一次警告；正常最多3s；后备后仅在未交互时移动同一 anchor 一次。
- [ ] 测试用户碰过不重定位、确认停止检测但不销毁 anchor、取消／退出完整清理。

### Task 4: 空间轮播与三种输入

**Files:** 新增 Flow/WorldCarouselLayout.cs、WorldCarouselSelectionController.cs、WorldCarouselSwipeHandle.cs、分支 UI 直接触碰适配器／小型 UI 共用构造器；修改 MoodiumSpatialInteractionController.cs；新增 WorldCarouselLayoutTests、WorldCarouselSelectionControllerTests、WorldCarouselInputTests。

**Interfaces:** Configure(两个Prefab、两个World、Camera)、ConfigureFonts、Begin／End、SetRecommendedWorld、SetMoodCheckSummary、SetMoodCheckSuspended；同贴纸的两个事件；`PointerStarted/PointerMoved/PointerEnded/CancelPointer`。

- [ ] 写纯函数失败测试：.06m阈值、取消返回、左右目标、边界、选中scale1/alpha1、侧项.7/.55、间距.34。
- [ ] 用现有 Prefab、GlassSlideToEnter、MoodiSelectionMascotController 和 tag.pick 构造轮播；仅中间项滑块可用／Moodi可见。
- [ ] 开始时前方.75m、眼下.12m，固定空间；转离50°平滑重新居中；切换.35s ease-out。
- [ ] 新控件全部是拖动区的兄弟；材质独立实例在 End 回收；字体补偿父级 lossyScale。
- [ ] 写选择／推荐／slider完成／End清理测试，entry=invalidId、empty ReferenceImageName、Anchor=null。
- [ ] 添加空间窄路由和鼠标捕获；给新分支滑块、按钮、拖动区及原校准 handle 桥接食指触碰。tracking lost/None/Cancelled/OnDisable 释放，不重复提交。
- [ ] 测试 slider 不被 swipe 截获、侧项点选、指针未过阈值返回、三种输入共享业务回调。

### Task 5: 分支流程、签到和切换

**Files:** 修改 MoodiumAppFlowController.cs、MoodCheck/MoodCheckController.cs、必要的贴纸入口小型共用 UI 挂接；新增 SensoryBagBranchFlowTests、扩展 MoodCheckFlowIntegrationTests。

**Interfaces:** AppFlow 统一选择入口仍用 BeginStickerWorldSelection；所有权切换回调；MoodCheckController.ShowRecommendCard（默认true）。

- [ ] 写集成失败测试：有袋走 ConfirmDefault；无袋无 tracking 订阅／manager enable，slider→finder→anchored calibration→同 OnCalibrationCompleted。
- [ ] 将选择组件初始化和 Coordinator 初始化分开；入口只 Begin 一个 selection；无袋停视频、隐藏 DisplayRoot，不创建贴纸提示。
- [ ] 在 SetExistingModules／世界输入配置／签到恢复处按当前分支保护 manager 和 spawner；无袋不从任何后续入口开启追踪。
- [ ] ShowRecommendCard=false 前测 Confirm 直接 Done，隐藏面板并发 Completed 一次；true 原行为不改；后测 Comparing 不受影响。
- [ ] 心情完成把推荐传到轮播并自动导航，不加自动签到；保留现有按需签到和退出返回。
- [ ] 次级切换按钮置于两种选关的心情入口下方；保存后 End 旧模式／Begin 新模式，保留本会话推荐。
- [ ] 测试返回分支、切换清理、确认去重、回调期间对象销毁、可选资源缺失容错与结构化日志。

### Task 6: 验证、截图和变更说明

**Files:** 新增 Assets/Editor/SensoryBagBranchValidation.cs、必要的 Editor 预览工具、docs/verification/2026-10-28-sensory-bag-branch.md、截图资源。

- [ ] Unity 编译并运行新分支／原贴纸／开机／校准／心情／空间输入相关 EditMode。
- [ ] 对照 baseline 修复本任务引入的回归；旧失败单列证据，不随意改其他功能以追求绿色。
- [ ] 跑全部 EditMode，记录准确总数、通过、失败、跳过与原因；git diff --check。
- [ ] 在独立临时预览中截糖果居中、自然居中、无袋校准三张图，恢复 Editor 场景布局，不保存覆盖当前场景。
- [ ] 变更说明列实际文件、Inspector 默认参数、次级按钮位置、已验与真机待验（平面权限、追踪、真实手势、视距和性能）。
- [ ] 最终交付本地修改，不 commit 或推送。

## 自查

上述任务覆盖原文 A–E、输入／UI／日志／容错与12组测试要求。没有加入新的自动签到、自动后测或更换世界艺术资源。实施前需用户审阅设计，尤其是现有按需签到行为与原文旧状态描述的差异。
