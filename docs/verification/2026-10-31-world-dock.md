# 世界悬浮按钮与改名包接入

在原 Unity 工程、预设心愿提交 4b93ae0e 上建立 codex/world-dock-1031。已读取并叠加 world-dock-1031.zip 的 README、AGENT_TASK、输入排查报告，以及 Moodium-WorldDockNames-1031.zip 的改名说明。按要求不推送远端。

## 已接入

糖果／自然世界房间阶段的手腕按钮与进度手环改为独立世界坐标的悬浮按钮。前方 50cm、低于眼睛 21cm，按钮各 10×3.7cm，中间间隔 2cm。转头超过 35° 持续 0.4s，或走动超过 30cm／高度改变 15cm 后，以 0.6s 滑动重新定位；手指靠近或刚按完时暂停跟随。低头／抬头不改变目标点。

左侧按世界显示「糖果雨 / Candy Shower」「花草雨 / Meadow Shower」，下雨中为「雨停啦 / Stop」，右侧为「退出世界 / Exit World」。未解锁显示细进度线与「能量集满就能下」，按下有抖动及继续捏的提示。能量满等待 2.5s 后按世界显示提示，按钮轻跳。进入泡泡时隐藏，返回后重新放到眼前；引导收集卡片飞入雨按钮 ReleaseAnchor。

默认一次点击退出；可选 m_ConfirmExit 打开后为 3s 内二次确认。玻璃贴图为默认 SpriteRenderer 材质，胶囊形命中网格是精灵的兄弟节点，承载系统 Hover，避免整张矩形贴图高亮。

同一 Press 入口接收既有中央派发器的捏合与 Touch。MoodiumSpatialButton 加 0.3s 去重，校准确认／重置／升降同样去重，拖动手柄不受该计时限制。指尖桥以执行顺序 -50 先于系统派发器释放自己占用的指针。旧 CandyEnergyWristUI 及测试保留，但当前运行时代码与场景／prefab 不再创建或引用它。

最终资源为改名包 47 张 PNG：40 张按钮、6 张提示与 1 张九宫格进度线。四个旧通用名字的贴图及 meta 均不存在。没有更改点云、点大小、点材质、GPU 运动或先前的风铃功能。

## 编译及额外边界修正

两个补丁均可直接应用。初次编译时，包内 WorldActionDockTests 引用 WorldJourneyStage 缺少 using Moodium.Flow，已补齐。其余接入检查修正：

- 原代码只在未滑动时检查手指距离，现允许手指接近时中断正在进行的滑动，并保持位置和旋转。
- 距离从手指到两个按钮碰撞体最近点计算，避免在按钮外侧接近时量到面板中心而漏判。
- 按文档将滑到位后的输入等待统一为 0.5s（原代码为 0.25s）。
- Suppressed、房间交互关闭、退出不可用时立即关碰撞体，集中 Press 检查也拒绝回调，不依赖视觉淡出结束。
- 停用时清理跟随与提示状态，重新启用会定位到新的正前方。
- 清理运行时胶囊网格和材质，抖动中停用复原位置；manager 销毁时释放独立 dock。
- 补充编辑器鼠标点按；生产跟随和按压状态机可注入时间/距离，测试运行真实状态机。

排查报告里的远距离面板拉近、开场多触点及其他 UI 推广建议没有纳入本次改动。

## 验证

Unity 6000.0.30f1，在隔离工程同步原代码与资源后编译/测试，再把新资源的 meta 同步到原工程。

- 新增测试 25/25 通过：WorldActionDockTests 13、WorldDockNamingTests 1、InputDoublePressTests 4、WorldDockIntegrationTests 7。
- 全量 777 项，748 通过、29 失败；对比预设心愿版本 752 项/29 失败，新增失败 0、原有失败集合不变。
- 集成测试驱动实际跟随状态机：50° 转头初期保持、延迟后到位、手指 10cm 内中断滑动；未解锁不触发奖励、解锁后触发一次；二次退出确认及超时；泡泡／房间切换立即禁用输入；返回和重新启用定位到新正前方。

[完整结果](world-dock-1031/full-tests.xml) · [新增测试统计](world-dock-1031/new-test-counts.json) · [失败对比](world-dock-1031/failure-comparison.txt)

## Unity 实际组件渲染

使用 WorldActionDock.Build、实际精灵/胶囊/进度线及 Tick 驱动状态。捕获环境中的墙、地板和桌面仅是临时几何示意；不是实机透视画面。每种状态包含近看及保持初始水平视线的布局图，共 24 张。没有导入包内 Source 工具或演示视频，捕获脚本仅以 .cs.txt 留档。

| 世界与语言 | 未解锁（55%） | 可用 | 下雨中 |
| --- | --- | --- | --- |
| 糖果 · 中文 | [近看](world-dock-1031/candy-zh-locked-close.png) | [近看](world-dock-1031/candy-zh-ready-close.png) | [近看](world-dock-1031/candy-zh-on-close.png) |
| 糖果 · 英文 | [近看](world-dock-1031/candy-en-locked-close.png) | [近看](world-dock-1031/candy-en-ready-close.png) | [近看](world-dock-1031/candy-en-on-close.png) |
| 自然 · 中文 | [近看](world-dock-1031/nature-zh-locked-close.png) | [近看](world-dock-1031/nature-zh-ready-close.png) | [近看](world-dock-1031/nature-zh-on-close.png) |
| 自然 · 英文 | [近看](world-dock-1031/nature-en-locked-close.png) | [近看](world-dock-1031/nature-en-ready-close.png) | [近看](world-dock-1031/nature-en-on-close.png) |

[初始视线布局示例](world-dock-1031/nature-zh-on-view.png)。已检查新名字、英文长标签、进度线和提示的尺寸/排布。

尚无 Vision Pro 真机验证。需实测双手捏合/Touch 每次只触发一次、校准升降不重复、滑块与拖动交接、按钮可发现性/可及距离、胶囊 Hover、真实跟随与暂停手感，以及进出泡泡和性能。编辑器渲染和测试不等于这些真机条件已通过。
