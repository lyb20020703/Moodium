# Device Test Fixes Design

## Goal

修复真机发现的五类问题：Moodi 语音全局串行、树点云密度与清晰度、RealityKit 预制体亮度、糖果世界地面贴图黑底闪烁，以及泡泡入口可发现性。

## Decisions

- `MoodiVoice` 使用静态全局仲裁器和 FIFO 队列。必说句最多排队 3 条，可省句在忙时丢弃；预计结束时间使用 `Time.unscaledTime + clip.length`，Emitter 与 Moodi 层级分离。
- 树点云使用 140000 点、PointSizeFactor 1.4、树干/树枝至少 12%，采样最大尝试为目标点数的 60 倍；树点使用 sharedGlow 图集布局并保留 2048 纹理上限。
- 预制体先采用材质托底方案：非金属部件 Metallic=0、Smoothness 0.3–0.5、Emission 0.15–0.3；果冻保留高 Smoothness。运行时记录材质修正日志。
- 地面贴图改用共享透明 Unlit MeshRenderer、无深度写入、固定排序和导入后处理；糖纸与地面内容使用稳定排序关系，离地高度 0.025m。
- 删除光环召唤入口的运行时生成，改为在互动结束且全局语音空闲后，从虚拟捏捏顶部召唤泡泡。房间返回后泡泡保留并可再次进入；手腕退出时销毁。

## Verification

新增 EditMode 覆盖全局语音队列、树采样预算/接受率/树干比例、图集大小、泡泡落点和入口时机。运行 Unity 编译与相关 EditMode；真机仍需验证亮暗环境、贴图闪烁、树性能和入口位置。
