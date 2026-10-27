# 捏捏救援接入

来源：Moodium-SqueezeRescue.zip，先阅读包内 README.md，补丁基于 195db9a。

## 接入内容

- 新增 MoodiRescueDirector、救援规则测试和 24 条中英语音；Unity 自动生成 .meta 并执行已有语音导入设置。
- 应用包内五文件补丁：果冻罩弹簧反馈、掉落与救援、低通语音、教学卡时序、散步向导交接。
- 保留现有能量和视频进度逻辑，不需要修改场景或预制体。
- 补充保护：没有向导 prefab 时不创建空白救援角色；跳出时清掉旧语音确保 K10 能播放；等待 K3 时同时等待排队语音，防止教学卡提前出现。

## 验证

Unity 6000.0.30f1，batchmode EditMode，筛选 MoodiRescueTests、MoodiVoiceTests、SqueezeProgressVideoControllerTests、WalkTrailGuideControllerTests。

首轮：36/36 通过，代码编译成功；增加语音排队回归测试后复测 37/37 通过，0 失败。git diff --check 通过。

尚需 visionOS 真机验证果冻弹性、低通滤波、空间位置及两个 Moodi 的视觉交接。EditMode 测试不代表已经完成运行时体验验证。
