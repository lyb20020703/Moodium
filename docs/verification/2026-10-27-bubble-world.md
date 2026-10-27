# 泡泡世界接入

来源：Moodium-BubbleWorld.zip。先阅读包内 README，再接入补丁和 Resources。

## 内容

- 自然与糖果世界各一套 20 万点点云、颜色图和全景，共用彩虹薄膜。
- 完成进度达到阈值后等待 10 秒，在用户左前方出现小世界泡泡。
- 支持指尖触碰和注视捏合，随后飞近、包裹、从近到远展开点云、淡入天空。
- CandyWorldManager 接入开始、进度和停止；保留现有捏捏救援、散步向导和完成小屋。
- 使用 BubbleWorldBaker.Bake 生成每个世界 20 层点网格及天空球，避免真机依赖运行时网格。
- 在包内测试基础上增加两世界的烘焙网格完整性、顶点数量和颜色图点采样检查。

## 验证结果

- Unity 6000.0.30f1 batchmode 编译和 BubbleWorldBaker.Bake 成功。
- 两个世界分别烘焙 20 个点层网格与 1 个天空球，共 42 个 .asset；各有 20 万点。
- BubbleWorldTests、MoodiRescueTests、MoodiVoiceTests、SqueezeProgressVideoControllerTests、WalkTrailGuideControllerTests 合计 56/56 通过，0 失败、0 跳过。
- git diff --check 通过。资源目录（含源数据和烘焙网格）约 85 MB。

## 真机待验

Vision Pro 上的网格/薄膜可见性、触碰手感、透明排序、地面对齐及 20 万点性能仍需实测。
