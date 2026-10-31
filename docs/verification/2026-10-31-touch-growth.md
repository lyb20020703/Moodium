# Moodi 触摸和世界生长音效

在原工程从呼吸音频接入版本 54e4b1f 创建 codex/touch-growth-1031。只应用输入 patch，没有用包内完整脚本覆盖。触摸补丁提交 32cbd8a，生长音效补丁提交 8232292；均干净应用，没有冲突。原泡泡讲解、F3 取消和呼吸音频接入保留。

MoodiCompanionAnimator 提供 PollHandTouch，RiseDisplay 接管时仍可检测触摸；Touched 播放时暂时关闭 Peeking/Emerging，之后恢复。触摸或播放反应期间抑制 Strain 下压；捏袋子其他位置保留用力反馈。没有修改 Touched 动画素材。触摸可能仍给袋子计一次能量，这是输入设计的明确取舍。

树生长改用 grow_nature，大小糖果屋改用 grow_candy，缺资源时退回旧音效。雨滴奖励和泡泡触摸音效不变。新 WAV 及 Unity 生成的导入 meta 已实际加入原工程。

Unity 6000.0.30f1 在独立副本编译和完整 EditMode 测试，原工程实际代码/音频已更新。全套 681 项：652 通过、29 失败；与 breath-audio-1031 的全套 XML 按测试名称对比，新增失败 0、消失失败 0。没有修改旧测试期望。[完整 XML](touch-growth-1031/full-tests.xml)、[29 项旧失败清单](touch-growth-1031/remaining-failures.md)。

指定套件：MoodiSoftTouchTests 4/4，GrowthSoundTests 3/3，MoodiAnimatedTests 14/14，WorldFix1030Tests 12/12。原有失败包括语音、轮播、心情流程、素材设置等，已逐项列明，未宣称全项目测试全通过。

真机尚未验证：探头时指尖触摸能否直观看到 Touched 且不缩小、动画结束回探头/冒出状态、袋子其他位置仍用力并涨能量、救出后触摸不受影响；糖果屋和树出现时的新声音、音量和连续生成时的叠加听感。EditMode 通过不等于设备手追踪或声音试听通过。

本地分步提交，未合并、未推送。原工程已有的其他 FBX 改动保留且未纳入此次提交。
