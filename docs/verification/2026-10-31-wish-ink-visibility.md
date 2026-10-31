# 风铃书写实时可见性修正

分支 codex/wish-ink-visibility-1031，基于 b13ca7e；修改原 Unity 工程，未推送远端。

## 代码检查

原实现已经在写字采样时更新 current 网格，并非只在 FinishStroke 时绘制。但有以下风险：

- 风铃根节点以 Order 4、ApplyToDescendants=true 整体注册；纸条、实时笔迹和完成笔迹没有明确排序差异。
- 笔迹主色和半透明光晕放在同一 renderer 的两个 submesh，距离/角度变化时依赖原生深度和透明排序。
- 贴面间距仅为局部 0.3/0.4mm，未按真实尺度和局部曲面法线计算。
- 单点不生成几何，保存采样还经过最小间距和半速平滑；落笔和尾部延伸缺少立即可见的反馈。

没有真机复现或设备 GPU 抓帧，因此不能把其中某一项宣称为已经实测确认的唯一原因。

## 修改

1. 纸条和模型逐 renderer 注册 Order 4；笔迹光晕为 5，主笔迹为 6，光标为 7。去掉风铃根节点的全体同序注册。当前/完成笔迹各分成光晕、主色两个轻量网格，避免混合透明/不透明 submesh。
2. 使用现有原生 URP Unlit、双面材质。实时和完成笔迹共用同一生成方法，绘制中与松手后外观保持一致；重写时主笔迹和光晕一起淡出。
3. 根据纸条局部曲面切向计算真实世界法线，主笔迹与纸面间距为 1.2mm，光晕为 0.8mm，不受 FBX、世界或书写放大尺度影响。
4. 首次落笔立即显示笔尖点。实时尾段使用当前笔尖，不等待保存采样；静止时避免反复重建。结束时保留实际显示的末端，防止松手后缩回。
5. 保留当前笔迹总计不超过 300 顶点、4000 点保存上限、原有音效/流程及玻璃按钮/泡泡防误触。网格修改后显式通知 PolySpatial renderer；没有新建每帧 Mesh，也没有改点云或世界素材。

原生排序规则依据项目当前 PolySpatial 的 Runtime/Public/VisionOSSortingGroup.cs；同步通知使用同包 PolySpatialObjectUtils.MarkDirty(Renderer)。Unity 的普通 Mesh 修改本身已有通知，额外调用作为 PolySpatial 显示更新保护，而非推断原实现完全不通知（[Unity Mesh.MarkModified 文档](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mesh.MarkModified.html)）。

## 验证

在隔离工程同步源代码后使用 Unity 6000.0.30f1 编译。新增测试覆盖两种真实纸条的落笔点、松手前网格、保存网格为空时实时线可见、笔尖延伸不污染存储采样、旋转/非均匀缩放/2.5倍书写尺度下的真实贴面间距、原生排序、材质双面性、顶点预算和重写淡出。另通过模拟右手持续捏合，在 WishChime.Update 中逐点书写并验证松手前已有网格、松手后保留笔尖终点。

针对性测试 44/44 通过，其中新增可见性测试 9 项。最终全量 730 项：701 通过、29 失败；对比上一版 721 项/29 失败，新增失败 0，原有失败集合不变。长笔划分段也预留一个终点采样位置，避免分段时笔尖缩回。

[完整结果](wish-ink-visibility-1031/full-tests.xml) · [针对性结果](wish-ink-visibility-1031/focused-tests.xml) · [失败对比](wish-ink-visibility-1031/failure-comparison.txt)

## 近远与斜视角渲染

使用实际风铃纸条和 WishInkMesh，在两种世界上分别渲染 0°、-55°、+55°，距离 0.35m、0.7m、1m，共 18 组视角。每组都保留空纸、刚落笔、未松手笔迹和完成笔迹四张图，共 72 张。未松手图由 Active() 生成，Completed 为空，不能靠完成笔迹补画。

18 组中落笔点均有可见像素，未松手笔迹均可见；相同几何从实时网格转到完成网格后，像素差异超过 8/255 的像素数均为 0。这是 Unity 渲染的静态网格对照，不是 Vision Pro 原生显示延迟或帧率测量。

- [糖果：未松手、侧面 55°、70cm](wish-ink-visibility-1031/Candy-a55-d70-live.png)
- [自然：落笔即显示、35cm](wish-ink-visibility-1031/Nature-a0-d35-dot.png)
- [自然：未松手、侧面 -55°、1m](wish-ink-visibility-1031/Nature-a-55-d100-live.png)
- [逐组像素对照](wish-ink-visibility-1031/pixel-checks.json)

捕获脚本和像素核对脚本仅以文本留档，不进入工程编译。

真机仍需验证：双眼立体显示、持续写字的原生更新延迟、正常观看距离与斜视角下的可读性，以及原生帧率。本次编辑器验证不能替代 Vision Pro 验收。
