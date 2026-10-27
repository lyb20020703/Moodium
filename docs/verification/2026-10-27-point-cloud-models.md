# 模型点云：CandyHouse 验证阶段

已阅读包内 README.md 和“真机验证：只把糖果屋换成点云”步骤，按验证阶段仅烘焙 CandyHouse。

- 新增 PointCloudFx、PointCloudModelAsset、PointCloudModel，支持下向上聚合、十指附近点散开、提亮和回弹。
- 在糖果完成模型及森林生成处接入 TryAttach；无烘焙资源的模型保留原网格。
- CandyHouse 烘焙结果：80000 点、20 块、512×512 颜色图、点宽约 1.1 cm（模型局部空间），资源约 25 MB。
- 新增单独烘焙 CandyHouse 和运行集成测试的菜单，避免验证阶段误烘焙所有模型。
- 增加烘焙网格和颜色图完整性测试。
- 首轮集成测试 60 通过、1 失败；失败定位到森林 StopAndClear 在 EditMode 使用 Destroy，已改为编辑模式 DestroyImmediate、运行模式 Destroy。

Unity 编辑器当前打开项目，因此在现有编辑器里完成编译、烘焙和 EditMode 测试；未关闭编辑器。

最终复测：61/61 通过，0 失败、0 跳过；git diff --check 通过。测试覆盖点云、完成小屋、旧网格交互、森林、泡泡世界和救援规则。结果文件：系统临时目录 Moodium-PointCloudModels-Tests.xml。

真机待验证：糖果世界到 80% 后点云可见、出现动画顺畅、十指触碰散开及回弹、帧率；92% 蜂蜜罐、100% 小糖果屋和自然世界树保持原网格。真机验证通过后再决定是否烘焙其他模型。
