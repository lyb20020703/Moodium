# 第二层入口直接进入：变更说明

## 文件与行为

- BubbleWorldController.cs：删除 EnterNestedWorld 和预览球模型，增加独立内层实例、Portal/HandingOff 阶段与共享 ApproachAndReveal 动画。外层资源在内层 Revealed 后释放，控制器保留转发事件。
- BubbleWorldNestedEntryTests.cs：检查半径淡出边界及单调性、入口到 Approach 的位置/半径连续性、阶段跳转、原房间落点和天空半径差异。
- 配套 spec 与 plan 在 docs/superpowers/specs 和 plans 下的 2026-10-29-nested-direct-entry.md。

## Inspector 参数

- Outer Fade Start Radius：1 m。
- Outer Fade End Radius：4 m。
- Nested Sky Scale：0.97。

内层加载在外层首次 Revealed 之后启动。入口至少等待 10 秒和核心层就绪；剩余层继续异步加载。没有改变已有 SourceFrameRotation 与地面位置修正。

## 验证与限制

Unity 6000.0.30f1 批处理编译完成。EditMode 共 37 项，34 通过、3 失败；新增交接测试 2 项及环境风测试 5 项全部通过。结果见 /tmp/moodium-nested-entry-verified.xml。控制器文件的 git diff --check 通过。

真机帧率尚未测量。仍需分别在自然、糖果世界检查双层切换时的帧率、天空连续遮挡、手势触碰、回房间和切换中退出。此次没有连接真机，不能报告实测帧率。

工作区原有地平线测试与资源存在不一致：AllWorldsUseCanonicalSourceFrames 仍读到非单位旋转；NestedBakedLayersMatchExportedCloud 的两组测试期望眼高 0.369 / 0.56，而资源为 1.35。这些资产转换不属于本次入口交接改动，保留现场以便继续处理。
