# 点云运动：阶段一静态替换记录

> 历史阶段记录：用户随后授权继续完成全部引擎实现。本文件保留静态阶段当时的范围；当前实现和最终验证以 `docs/validation/bubble-motion/README.md` 为准。

## 本次范围

- `Assets/Shaders/BubbleSplatMotion.shadergraph`：Universal Unlit、Transparent / Alpha、Both、Depth Write Off、Alpha Clipping 开启且阈值为 0。UV0 采样颜色图并乘以 `_BaseColor`，分别连接颜色和 Alpha 输出。顶点保持原位置。
- `Assets/Resources/MoodiumBubbleWorld/BubbleSplatMotion.mat`：直接引用 Shader Graph，保留方案全部 14 个参数，风和手强度为 0。
- `BubbleSplatMaterial.cs`：每个世界复制独立材质，设置颜色图、扫描眼高和世界类型，输出实际着色器日志。资源缺失或着色器不符时警告并回退 URP/Unlit。
- `BubbleWorldController.cs`：使用新材质；静态模式不创建旧风骨骼和手部扰动，后备材质仍保留旧行为。现有整体淡出、嵌套交接和呼吸缩放保持原逻辑。
- `Assets/Editor/BubbleSplatMotionSetup.cs`：用于当前 Unity / Shader Graph 版本的静态资源生成器及 MaterialX 转换检查。菜单会重建静态 Graph，后续动态阶段须同步更新生成器。
- `Assets/Editor/BubbleSplatMaterialTests.cs`：资源参数契约、独立材质淡出及 MaterialX 转换测试。

本阶段没有加入运动公式、UV1、Sub Graph 或双手驱动，也没有重烘焙网格。旧组件及测试保留至动态方案通过真机验证后删除。此前开机按钮上移修改保留。

## 本地验证

- Unity 6000.0.30f1 batchmode 脚本编译、Graph 导入和资源生成成功。
- 已检查序列化 Graph：三个片元输出均有实际连线；颜色来自纹理乘色，Alpha 来自结果的 A 通道，阈值来自常量 0。
- PolySpatial 2.4.3 `MtlxPostProcessor.ProcessGraph` 转换成功。日志未见此 Graph 的不支持节点或转换警告。工程有既有的未使用字段、空程序集以及无图形模式探针着色器提示。
- EditMode 测试 35/35 通过：`BubbleSplatMaterialTests`、`BubbleWorldTests`、`BubbleWorldNestedEntryTests`。原始日志位于 `/tmp/moodium-static-splat-build.log`、`/tmp/moodium-static-splat-tests.log`，结果 XML 位于 `/tmp/moodium-static-splat-tests.xml`。
- 本次尚未生成新的 visionOS/Xcode 构建，Editor 转换成功不代表已通过设备渲染验证。

## 真机门槛（未完成）

`xcrun devicectl list devices` 列出的 Apple Vision Pro BBB、VisionA1、VisionA2 均为 unavailable。未取得设备截图、录屏、Xcode 运行日志或帧率数据。

设备可用后，构建并在自然、糖果、两个第二层中逐项检查：

1. Xcode 日志必须出现 `[Bubble Motion] point shader = Moodium/BubbleSplatMotion`；若为 URP/Unlit，排查回退警告。
2. 与旧版比较颜色、圆点柔边、透明度和排序；静态阶段无粒子运动是预期行为。
3. 检查进入、退出、防走动淡出以及第二层包裹时的外层淡出。
4. 检查完整设备构建日志没有该 Graph 的 PolySpatial 转换警告。

输入文档第 7 节明确要求“每一步都要上真机确认后再进下一步”。静态阶段验收完成后，才进入 UV1 / bounds、动态顶点位移及 Boost=8 调试阶段。
