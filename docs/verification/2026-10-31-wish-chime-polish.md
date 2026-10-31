# Wish Chime Polish 1031

依据 Moodium-WishChimePolish-1031.zip 的 README 和 AGENT_TASK，在 1af429c8 上建立 codex/wish-chime-polish-1031，修改原 Unity 工程。按交接包要求本次不推送远端。

## 按钮

导入 22 张中英文液态玻璃按钮图及专用导入器：996×564、PPU 7333.33、无压缩、mipmap、Trilinear、透明边、居中 FullRect。按钮胶囊本体约 9×3.1cm；两中心相对纸条中心左右各 12cm。保留原 FBX、纸条、笔迹和交互流程，删除旧平板网格/TMP 按钮。使用默认 SpriteRenderer 材质，不添加系统 Hover 高光或自定义着色器。按钮 Order 5。

空纸条的放飞按钮 disabled；重写无 disabled 图时降至 45% 透明。绘制中、结束后 0.35s 内、擦除中、保存中均拒绝按钮。支持编辑器鼠标悬停贴图、点击及真机既有空间按钮回调，按下反馈 0.15s。

## 泡泡让位与防误触

- 空白风铃或书写纸条周围水平 35cm 加泡泡半径为目标禁区，保持距离/高度绕到侧面。
- 书写、飞往挂点或手靠近空白风铃 25cm 内进入专注；结束后保留 1.2s。
- 泡泡停靠在空闲手一侧 50°、距头 0.6m、低 0.15m；约 0.6s 到位、缩至 70%、膜透明度乘 55%。停靠点固定，仅离视线超过 80° 时重新计算。
- 下一层入口和退出泡泡均有集中防护：普通指尖触碰、注视捏合和鼠标点击不能绕过专注锁；关闭对应 collider。
- 锁住时连续指尖停留 0.6s 才进入/退出；移开立即清零。原生 Unlit、48 段程序网格绘制主题色细光环，无 LineRenderer；与膜加入同一排序组，Order 高一级。
- 解除后约 0.8s 回位并恢复外观；有 0.8s 触碰宽限，且必须先移开再碰。风铃 Leave 会清理专注和写字手状态；退出/清理释放光环网格和材质。
- 手采样、风铃专注、控制器按顺序更新；书写状态也直接参与集中锁，避免开始书写的首帧空档。

没有修改点云数据、网格、点大小或点材质，没有启用 GPU 运动。

## 验证

Unity 6000.0.30f1 隔离副本编译通过；资源 GUID/导入生成的 meta 已同步回原工程。

- 针对性测试：37/37 通过（风铃、玻璃按钮、防误触、嵌套进入）。
- 最终全量 EditMode：721 项，692 通过，29 失败。对比 695 项/29 失败的 BubblePlay 基线，新增失败 0，原有失败仍为同一组。
- 包内 WishChimePolishTests：16/16；新增 WishChimeGuardTests：10/10。
- 覆盖全部贴图尺寸/路径、状态优先级、纸条间隙、左右手停靠/禁区、专注尾巴、连续按住、锁住时普通与 force 进入/退出、宽限期重新离手、按钮各项阻塞及 disabled 不调用动作。
- 旧 RealDevice1030bTests 曾按源码字面匹配旧 armed 分支，已改为验证共享门控的离手行为，并保留入口/出口接线检查；没有删除测试。

[完整结果](wish-chime-polish-1031/full-tests.xml) · [针对性结果](wish-chime-polish-1031/focused-tests.xml) · [基线失败对比](wish-chime-polish-1031/failure-comparison.txt)

## 渲染截图

使用实际 WishChime / WishActionButton / BubbleHoldRing 组件渲染。停靠截图调用生产 UpdateFollowTarget，以书写状态模拟停靠并展示 60% 按住进度；泡泡仅渲染膜，未实例化缩略点云。背景使用包内预览 JPG，仅用于临时编辑器截图，没有导入生产资源或替换任何世界点云。截图不是完整运行流程或真机录屏。

| 世界 | 按钮 | 模拟书写停靠 |
| --- | --- | --- |
| 糖果第一层 | [截图](wish-chime-polish-1031/Candy-buttons.png) | [截图](wish-chime-polish-1031/Candy-focus.png) |
| 糖果第二层 | [截图](wish-chime-polish-1031/Candy_Nested-buttons.png) | [截图](wish-chime-polish-1031/Candy_Nested-focus.png) |
| 自然第一层 | [截图](wish-chime-polish-1031/Nature-buttons.png) | [截图](wish-chime-polish-1031/Nature-focus.png) |
| 自然第二层 | [截图](wish-chime-polish-1031/NestedNature-buttons.png) | [截图](wish-chime-polish-1031/NestedNature-focus.png) |

临时捕获脚本以 .cs.txt 保存在记录目录，不接入工程编译。没有进行 Vision Pro 真机验收：仍需确认最亮背景下的文字、手扫按钮、碰撞穿过泡泡、连续按住/移开取消、转身重新停靠及解锁回位的手感与帧率。
