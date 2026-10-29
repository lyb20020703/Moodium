# Nature World 满能量环境藤蔓接入

## 复用与修改

- 复用 `NatureIvySpatialGrowthController`、`NatureIvySurfaceResolver`、`CandyWorldManager` 的既有创建/结束/泡泡隐藏流程。
- 场景 `Assets/Samples/PolySpatial/Scenes/Meshing 1.unity` 已绑定 `Procedural Ivy Long EDITOR SOURCE URP.prefab`。保留三维枝干与 URP/Lit 材质、现有 PolySpatial 叶片材质，不开启插件自定义 growth shader。
- `MoodiumSpatialInteractionController` 在既有 SpatialPointer Began 分支转发 IndirectPinch。只处理没有虚拟目标或目标属于 AR 网格的事件；UI、模型及原有玩法优先，不穿透这些交互触发藤蔓。
- 优先使用系统提供的 startInteractionRayOrigin/Direction；不可用时，只允许已确认的 AR collider 与 interactionPosition 构造命中射线。未使用手指射线或头部 forward 冒充视线。
- `CandyWorldManager` 仅在自然世界传递 Energy 进度；达到 1 后锁存解锁。控制器另要求 Journey=Explore、正在运行且未挂起。进入 Bubble/Ending 禁用，回房间恢复，退出/新关卡重置。
- 手动预算从 3 提升为 48 组/会话。满能量既有自动藤蔓 burst 不消耗手动预算。
- 删除无 ARMeshManager 时收集全场景 MeshCollider 的兜底，防止误认虚拟模型。起点 raycast 及插件所有枝干寻路 raycast 都限定到 ARMeshManager 下的 MeshCollider；不要求法线朝上，因此允许墙面。原有向下寻找桌面的方法保留用于原有完成奖励。
- IvyGeneratorTREANT 新增每实例 surfaceRaycast 回调与 requireSurfaceAttachment；仅本控制器启用，其他插件用户沿用默认实现。找不到附着面时截断枝干，不继续使用原插件自由悬垂分支。
- 动画仍使用 IvyStudio 算法生成的三维藤蔓，按根到梢的三角形顺序在约 1.5 秒内逐段显示；不缩放整棵植物、不替换成粒子。叶片在枝干显示完成后出现，只采样真实引用的顶点，避开插件预留的零顶点。进入泡泡暂停生长，回房间续播。
- StopAndClear 停止协程、释放新生成 Mesh、断开查询回调并销毁 generator 对象及叶片。

## 配置与限制

当前场景沿用已有 Prefab、ARMeshManager 和材质引用，无需新增 Inspector 引用。其他场景如果缺少 ARMeshManager/碰撞网格或未配置 Ivy Prefab，将不会生成，不会用虚拟物体代替。

PolySpatial 当前包的 `Documentation~/PolySpatialInput.md` 指明系统初始视线射线仅在 unbounded 模式和输入开始时可用，并需要 collider 接收输入。这里复用 Began 时的系统数据；真实设备上的 AR 网格输入可达性仍需验证，不能从桌面 raycast 测试推断。

本次没有改动点云、风、选关 UI 或其正在进行的其他修改。

## 验证

Unity 6000.0.30f1 编辑器实际重新编译；通过 TestRunnerApi 运行 NatureIvySurfaceResolverTests、NatureIvySpatialGrowthControllerTests。测试结果见同目录 XML。

自动检查覆盖：小于 100% 拒绝、100% 解锁、Explore/Bubble 切换、挂起、停止重置、非 AR 表面拒绝、墙面射线越过无关虚拟 collider、空方向无命中、原桌面筛选、实际 IvyStudio 生成及预算、保留三维几何且不启用自定义增长 Shader。

未进行 visionOS 构建、设备部署或真机运行。需在 Vision Pro 验收：注视地面/墙面/桌面远距离 pinch；持续 pinch 只生成一次；模型与 UI 不同时触发环境藤蔓；扫描边缘停止生长；连续多个位置生成；进入/退出泡泡暂停恢复；退出关卡无残留；动态网格同步、材质、帧率及生长观感。
