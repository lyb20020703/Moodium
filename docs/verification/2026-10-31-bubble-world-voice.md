# 泡泡世界讲解实施与验证

基于 41425c27，在原工程新建 codex/bubble-world-voice-1030，原补丁提交 dbf8d91。四个世界八段中英文语音、LayerRevealed 转发、2D Narrator 和 F3/F4 等待讲解的逻辑已接入。每层同一世界会话一次，重进泡泡不重复，新世界会话重置；离开或走远时停止/淡出。

补充修复：原补丁 Speak 在第二层切换时直接 Stop，未满足输入说明的淡出要求。改为先将正在播放的源在 0.5 秒内淡出，再等待 1 秒开始下一层讲解。没有更改语音素材、背景音乐音量或点云资源。

呼吸包依赖情况：已导入 breath_inhale.wav 和 breath_exhale.wav 及 Unity 生成的 meta，位置按 README。当前工程未包含输入说明所引用的 Moodium-BreathSound-1030 播放代码，因此两段音频尚未接入呼吸模式播放；没有宣称这部分已完成播放验收。

Unity 6000.0.30f1 在独立验证副本编译与全套 EditMode 测试，原工程代码和音频均已实际修改。BubbleWorldNarratorTests 5/5 通过。全套 674 项：645 通过、29 失败；相对 nested-footprint 全套清单新增失败 0、消失失败 0。[完整 XML](bubble-world-voice-1030/full-tests.xml)、[旧失败清单](bubble-world-voice-1030/remaining-failures.md)、[输入说明](bubble-world-voice-1030/requested-readme.md)。

真机尚未试听：两世界两层讲解触发、快速切层淡出、F3/F4 不重叠、F5/退出淡出、重进不重复、切换新世界重置、中英语言、背景音乐与人声音量比例均待设备确认。自动化通过不等于听感或设备音频时序验收。本次未合并、未推送。
