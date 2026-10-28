# 感官袋有／无双分支设计

## 状态：待用户审阅

依据用户提供的 sensory-bag-branch-agent-prompt.md。原文保留了“写完 spec 后停下，等我确认”，因此本轮只完成设计和实施计划；未修改运行时代码。不得 commit、reset、revert、clean、stash，不覆盖工作区已有的点云、按需教学等改动。

## 目标与范围

- 有袋：开机选择有 → 原贴纸选关 → 原进入滑块 → 默认校准确认路径 → 世界；不显示校准框，不播放校准视频。
- 无袋：开机选择没有 → 空间轮播 → 原进入滑块 → 桌面检测／后备桌面 → 原校准框和校准视频 → 确认 → 世界。
- 两种选关都保留心情签到、世界推荐、即时切换有／无感官袋和返回原分支。
- 不新增 ARSession，不改变校准数学、心情表盘、反馈／能量、XR Hands 主系统或已有艺术资源。
- 延续当前的按需心情签到与退出直接返回行为。文档关于“后测和对比卡不变”解释为保留当前项目行为及其可复用状态机，不恢复已经移除的自动后测或开机自动签到。

## 已核实的现状

1. OpeningVideoHardwareChoice 负责实际的 Yes／No 选择；OpeningManager.PlayHardwareTutorial 在选择后请求 BeginEntryMoodCheck，再播 Yes／No 分支视频。当前 BeginEntryMoodCheck 仅关闭面板，不主动弹签到。
2. MoodiumAppFlowController.BeginStickerWorldSelection 当前依赖 OpeningVideoSession，始终启动 StickerWorldSelectionController 和 ARTrackedImageManager。
3. OnStickerWorldEntrySelected 始终进入 SensoryBagCalibrationCoordinator.BeginCalibration；确认事件 OnCalibrationCompleted 进入同一世界生成路径。
4. Coordinator 只有图像锚点入口，以 trackableId 管理 session／presentation；CreateDefault 已定义 40×25 cm、5 cm 厚、贴纸下方 5 cm、Euler(90,0,0) 校准面转换。不会读取跨启动的保存校准值。
5. SelectObjectWorld 调用 SetExistingModules(true,false)，目前会再次开启 image／object tracking；这必须按分支保护，不能只改选关入口。
6. MoodCheckController.Confirm 的前测必经 Recommending；需要增加开关，而不是让轮播自行截获或伪造样本。
7. GlassSlideToEnter 已有 PointerStarted／Moved／Ended；空间指针有路由，但通用编辑器鼠标入口没有完整覆盖此滑块。新分支必须补齐编辑器鼠标和直接手指输入。
8. 当前没有统一的运行时设置面板；语言选择属于开机流程。因此按原文允许的后备方案，在两种选关界面心情入口下方放次级文字切换按钮。

## 方案选择

采用文档指定的“平行选关控制器，共用后半段确认与生成”方案：保留 StickerWorldSelectionController，新增 WorldCarouselSelectionController。两个控制器均发出 Action<StickerWorldEntry> WorldEntrySelected、Action MoodCheckRequested。

不把轮播伪装成 ARTrackedImage，不给无袋用户构造虚假 reference image 或 trackable；不另写 Soft Prefab 摆放代码。另一种将两种 UI 全塞进贴纸控制器的方式会增加原追踪路径的回归面，不采用。

## 1. 选择持久化与分支状态

新增 Moodium.Flow.SensoryBagOwnership：

- Key = "Moodium.HasSensoryBag"；HasBag 属性默认 PlayerPrefs.GetInt(Key,1)==1。
- SetHasBag(bool)：仅写入 0／1、Save，并输出结构化分支日志。
- 在硬件按钮实际确认时写入；跳过开机不改已存值。用户在选择按钮后直接 Skip 也保留该次选择。
- AppFlow 对当前选关分支作会话快照，避免拖动中因外部 prefs 变化混用两条路；次级按钮通过 AppFlow 显式重启分支。

## 2. 有袋默认确认

Coordinator 增加 ConfirmDefault(StickerWorldEntry entry)：

- 有效世界、真实 Anchor；从 entry.ReferenceImageName／World 推断 WorldKind。
- 调用与 BeginCalibration 完全相同的初始数据构造函数，统一使用 CreateSettings + SensoryBagCalibrationData.CreateDefault。
- 共用完成方法：保存 confirmed data、CreatePresentation、发 CalibrationCompleted；保证一个入场请求只确认一次。
- 不创建 SensoryBagCalibrator，不启动 FrameAdjustingTutorialPlayback；忽略 m_ConfirmedData 中先前结果。
- 默认值严格保留 Width=.40、Height=.25、Depth=.05、CenterOffset=(0,0)、PlaneLocalOffset=(0,-.05,0)、LocalOrientation=Euler(90,0,0)。
- OnCalibrationCompleted 继续使用原路径，停止检测和校准模式后 SelectObjectWorld。软模型底座仍由 CalibratedSensoryBagPresentation.FitVisualToCalibration 对齐。

## 3. 无袋轮播

新增 Flow 下三个职责清楚的组件：

- WorldCarouselLayout：纯计算滑动阈值、目标索引、项布局位置／旋转／缩放／透明度。
- WorldCarouselSelectionController：生成和回收空间 UI，当前项／推荐项／动画／重新居中／签到暂隐；复用两个选关 Prefab、GlassSlideToEnter、MoodiSelectionMascotController 和现有推荐文案。
- WorldCarouselSwipeHandle：按 pointer id 捕获拖动，开始／移动／结束／取消；模型项附带 tapIndex，短点侧边项也可切换，中间项点击不等于进入。

布局：根对象处于会话空间，放在用户前方 .75 m、眼下 .12 m；项间距 .34 m，侧项 scale=.7、alpha=.55、向中心旋转 20°。选中项 scale=1、alpha=1；仅选中项显示滑块和 Moodi。左右箭头使用简单符号，不加说明文本。推荐标签只出现在推荐世界模型上。

拖动阈值 .06 m，松开后判定；不足阈值归位；超过阈值或点击侧项，在 .35 s ease-out 内切换。两项边界不向空项滚动。推荐改变时可直接导航到对应项。初始从有效 PreSample.recommendedWorldId 选择，否则 candy。

所有控件按独立兄弟节点组织：模型视图区／拖动区／滑块／心情入口／切换按钮，滑块和按钮不得成为拖动区的子节点。模型材质 alpha 使用独立实例，不改 Prefab 的共享材质；结束时回收材质。字体通过 ConfigureFonts，文字统一做 GetCompensatedLocalScale 补偿。

视线转离根位置超过 50° 时，平滑移动到新的前方位置；完成后再次固定，不逐帧跟头。签到期间隐藏并禁用轮播交互，结束后恢复。

轮播事件入口：TrackableId.invalidId、Anchor=null、ReferenceImageName=empty。找到桌面后由 AppFlow 创建同世界的校准 entry（真实合成 Anchor、invalidId），作为 Coordinator 与 OnCalibrationCompleted 的共同身份，避免修改不可变 StickerWorldEntry 或丢失回调身份。

## 4. 平面选择与锚点

新增 Calibration.TablePlaneSelector 纯函数和 TableAnchorFinder 运行时适配层。

候选包含世界 Pose／上方向、水平面标记、Table／Floor 分类、局部边界 Vector2[]。只接受向上的水平面；拒绝 Floor，优先 Table，其次未分类。候选必须在眼下 .2–1.0 m、水平距离≤1.5 m、前方±40°。按距用户前方 .5 m 目标点最近排序；同类多候选取最近者，排序不受 AR collection 顺序影响。

落点计算：将目标点投影到平面坐标；在多边形内部则保留，否则取距目标最近的边界线段点；缺少边界时使用 ARPlane 的 extents 矩形。最终校准锚点在落点上方 .05 m。锚点 local Y 朝上，平面 X/Z 与用户水平左右／前后对齐，正面朝向用户；相同校准 LocalOrientation 保持框在桌面上。

TableAnchorFinder 仅复用现有 XROrigin 上的 ARPlaneManager；没有则加组件且默认关闭，planePrefab=null、requestedDetectionMode=Horizontal。查找期间显示双语 calib.searching。无可用 subsystem／权限时给一次警告并立即后备；正常运行最多等 3 s。

后备桌面：用户前方 .5 m、眼下 .45 m；锚点眼下 .40 m。后备后检测仍可短暂服务当前校准；若之后找到桌面且用户未交互，将同一个 Anchor 移动过去一次，不重开校准、不重播教学视频。任何框拖动、边角调整、升降、Reset、Confirm 都标记 HasUserInteracted；一旦碰过不再自动移动。校准确认、返回、退出、分支切换都关闭 manager 与查找提示。锚点必须存活到其 presentation 被清理，不能在确认时一起销毁。

Coordinator 增加 BeginAnchoredCalibration(entry, anchor)，允许空 reference image，根据所选 world 创建默认校准数据；同原校准确认回调与视频路径。

## 5. AppFlow 生命周期与追踪隔离

- BeginStickerWorldSelection 保留名字作为既有入口，但内部按所有权分派；两条分支启动前清理两个 selection controller、旧 finder、校准 session 和 presentation。
- 有袋：原贴纸订阅在 manager enable 之前，原视频循环与提示不改。
- 无袋：End sticker、Stop warmup、停 image spawner、禁用 ARTrackedImageManager／ARTrackedObjectManager；Pause video 并隐藏 DisplayRoot；仅 Begin carousel。后续 SelectObjectWorld／SetExistingModules 不得重新开启任一 image tracking 输入。
- RequestStickerMoodCheck：按当前 selection 暂隐与恢复；只有有袋恢复时才 enable manager／启动 warmup。
- OnMoodCheckCompleted：更新 context 与两种选择的 summary/recommendation；无袋确认后轮播导航到推荐世界。
- OnStickerWorldEntrySelected：结束当前选关；配置共用 Soft Prefab／字体／视频；有袋 ConfirmDefault，无袋启动 finder → BeginAnchoredCalibration。
- 返回世界仍进入 BeginStickerWorldSelection，在该入口读取当前所有权分支；不强行回贴纸。
- Settings 次级按钮 End 当前分支、保存选择、Begin 另一分支，不丢弃当前 PreSample。

## 6. 心情状态机

MoodCheckController 新增 ShowRecommendCard，默认 true。前测 Confirm 时，为 true 维持原卡；为 false 直接 State=Done、隐藏面板、Completed(sample) 仅一次。后测 Comparing 及 ContinueComparison 不受开关影响。

AppFlow 在启动手动前测前按分支设置该开关。有袋保留推荐卡；无袋省略贴纸引导。没有在轮播里新增 12 秒自动签到计时器；现有 MoodEntryChip 只提示不自动打开的行为不扩展成自动弹窗。

## 7. 输入与文案

- 空间指针：MoodiumSpatialInteractionController 增加仅匹配 WorldCarouselSwipeHandle 的窄路由，按 interactionId 捕获；正确处理 Moved／Ended／Cancelled／None 和 OnDisable 清理。
- 编辑器：新增轮播与 GlassSlideToEnter 鼠标捕获；使用启动时的拖动平面投影，松手／跟踪取消都释放。
- 直接触碰：新增仅用于分支 UI 的手指输入适配器；食指世界坐标使用现有 PalmPressGestureController.TransformTrackingPose。桥接到相同的滑块／拖动／按钮／校准 handle API，不另写业务行为；tracking lost、组件隐藏时取消捕获。
- 全部可点击元素有 Collider 和系统 VisionOSHoverEffect。直接触碰进入触发一次，退出后才重新武装；避免与 gaze+pinch 双重触发。
- 新文案加入 MoodCheckCopy：calib.searching、bag.switch.toNone、bag.switch.toHas、settings.bag.title；标签和世界名沿用已有键／格式。

## 8. Inspector 默认参数

轮播：Distance=.75、DropBelowEyes=.12、ItemSpacing=.34、SideScale=.7、SideAlpha=.55、SideYaw=20°、SwipeThreshold=.06、SwitchSeconds=.35、RecenterAngle=50°。

找桌面：WaitSeconds=3、HalfFov=40°、MaxHorizontalDistance=1.5、MinDrop=.2、MaxDrop=1.0、ForwardDistance=.5、FallbackTableDrop=.45、AnchorLift=.05。公开检测／交互状态供 EditMode 通过可注入候选验证，不要求 XR subsystem 才能测试纯规则。

## 9. 验证与交付

- 开始实施前运行现有相关测试记录基线；检查到现有 MoodCheckFlowIntegrationTests 仍有“自动后测”断言、StickerWorldSelectionControllerTests 有旧式滑块层级断言，与当前代码不符。若基线证实，单列记录，避免把历史失败归因新分支；只在与本任务直接相关的契约变化处更新断言。
- 新测试覆盖：默认持久化／开机选择／切换；默认确认与未改动的 Begin→Confirm 字段一致；不出现校准 UI／视频；Soft 底座；轮播选择、阈值、滑块唯一性、推荐、清理；无袋前测只发一次 Done；平面过滤、分类优先、落点边界、后备、一次自动贴桌面／用户交互保护；无图像订阅、返回和即时切换。
- 编译通过后跑相关测试，再跑全部 EditMode。对实际基线失败、可选资源缺失单列说明，不宣称真机已验收。
- Editor 输出三张图：糖果居中、自然居中、无袋校准框。使用独立预览场景或临时预览对象，不保存改写用户当前场景。
- 变更说明列文件、参数、切换位置、测试结果、真机待调项。所有分支日志统一 [Sensory Bag Branch]，包括 mode、step、fallback／reason，避免逐帧刷日志。

## 审阅重点

1. 当前按需签到和退出直接返回保持不变，原文的后测“不变”不解释为恢复旧自动行为。
2. 感官袋切换放选关界面心情入口下方，而非新建设置面板。
3. 无袋先定位／校准、确认后进入已有世界生成路径；寻找和校准阶段不运行 image tracking。
