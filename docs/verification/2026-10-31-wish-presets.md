# 预设心愿 1031 接入与验证

基于 8de6c1b3，在 codex/wish-presets-1031 分支修改原 Unity 工程。读取用户提供的 Moodium-WishPresets-1031.zip 的 README、AGENT_TASK、补丁及源码后实施；未推送远端。

## 已接入

左侧五句预设心愿与「自己写」液态玻璃小签；中间保留原纸条，右侧放飞在上、重写在下。中英文跟随现有语言设置。五句为平安喜乐、天天开心、好事发生、身体健康、烦恼退散。

选择后按真实笔划逐笔写入原 WishInkMesh，约 2.2–3.4 秒。预设期间手写落笔和摇晃重写被屏蔽；允许换另一句、重写、切回自己写及提前放飞。完成后轻响风铃并令放飞按钮轻跳两次。默认 AutoReleaseAfterPreset=false，不自动放飞。

预设使用原 List<List<float[]>> 笔迹格式和原有本地原子存档/放飞/挂起/LOD 流程，增加可选 preset 字段，旧存档兼容。预设线宽为手写的 0.7，光晕为 0.5；恢复存档后维持相应线宽。预设后可手写签名，再点同一句不会清掉签名。选择自己写会清掉预设和附加签名；纯手写内容保留。

保留先前实时笔迹、贴面间距及原生排序修复。B1/B2c/B2n 仍停用；首个空白风铃仍在讲解结束约 2 秒后出现。没有修改点云、点大小、点材质或启用 GPU 运动。

## 接入时修正的边界

补丁可直接应用，C# 编译未报错。代码检查另外修正：

- 自动书写刚开始、尚无完整笔划时，放飞按钮和托起手势也允许完成后放飞。
- 动画中途切换、重写或自己写时保留实时半笔直到淡出，避免淡出前突然消失。
- link.xml 增加 WishPresetDocument/WishPreset，保留 IL2CPP 反射读取的字段。
- Garden 支持可选存档路径，默认保持原用户目录；集成测试写入唯一临时目录，不触碰用户风铃数据。

## 素材与许可

导入 56 张 Chips 精灵、wish_presets.json、三个运行时脚本与导入器。PNG 使用默认 SpriteRenderer 材质、无系统矩形 Hover 高光。Source/生成工具、预览视频和源字体没有导入生产资源。

第三方说明与 Arphic 许可已放入 Assets/ThirdPartyNotices/WishPresets，未放入 Resources；英文 EMS Felix 来源及许可注明在 NOTICE。未发现现有应用内致谢/开源许可页面，未增加新的设置入口。

## 验证结果

Unity 6000.0.30f1，隔离副本同步当前代码/资源后验证，并将新资源导入生成的 meta 同步到原工程。

- 针对性 EditMode：66/66 通过。
- 包内 WishPresetTests：14/14；新增 WishPresetIntegrationTests：8/8。
- 全量：752 项，723 通过，29 失败；对比 730 项/29 失败基线，新增失败 0、原有失败集合不变。
- 覆盖五句×双语数据、56 张贴图、版式与点击范围、笔划/笔顺时间线、深拷贝、线宽、新旧存档、动画完成逐点一致、动画中放飞的实际临时文件存档、手写输入屏蔽、自己写清理规则、首笔切换淡出以及恢复后的线宽。

[完整结果](wish-presets-1031/full-tests.xml) · [针对性结果](wish-presets-1031/focused-tests.xml) · [基线失败对比](wish-presets-1031/failure-comparison.txt)

## Unity 实际组件渲染

使用现有纸条、WishPresetPanel、WishChime.StepPreset 时间推进及 WishInkMesh 渲染选中态和笔迹。背景取自包内预览 JPG，仅临时用于捕获，没有加入生产工程。截图不是完整真机流程录像；未通过手部跟踪驱动选择。临时捕获脚本仅以 .cs.txt 留档。

| 世界 | 写到一半 | 写完 |
| --- | --- | --- |
| 糖果第一层 · 平安喜乐 | [截图](wish-presets-1031/Candy-half.png) | [截图](wish-presets-1031/Candy-complete.png) |
| 糖果第二层 · 天天开心 | [截图](wish-presets-1031/Candy_Nested-half.png) | [截图](wish-presets-1031/Candy_Nested-complete.png) |
| 自然第一层 · 身体健康 | [截图](wish-presets-1031/Nature-half.png) | [截图](wish-presets-1031/Nature-complete.png) |
| 自然第二层 · 烦恼退散 | [截图](wish-presets-1031/NestedNature-half.png) | [截图](wish-presets-1031/NestedNature-complete.png) |

未进行 Vision Pro 真机测试。仍需确认点击签的手感、动画/光点延迟、中文复杂字与英文远距清晰度、写完按钮跳动、音效和整体帧率；自动化与编辑器截图不能替代这些检查。
