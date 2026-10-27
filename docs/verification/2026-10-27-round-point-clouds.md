# 柔边圆点与全模型点云

来源：新版 Moodium-PointCloudModels.zip。用户已验证 CandyHouse 动画，并明确要求修正模型与泡泡世界的方块颗粒、扩展其他模型。

## 改动

- 根因：旧模型和扫描网格的四个顶点使用同一颜色像素，没有逐点 alpha 形状，因此整张方片可见。
- 模型采用新版包的 4×4 柔边颜色块（原色/提亮色）、分角 UV、Bilinear、无 mipmap、未压缩 RGBA；材质 URP/Unlit 开启 Alpha Clipping，Cutoff=0。
- 泡泡世界补上同一柔边方法：由 points.bytes 颜色生成 point_colors_round.png，每点一个 4×4 色块，两个世界各 2048²，重新烘焙全部 42 个网格。
- 五个模型全部烘焙：CandyHouse、CandyLittleHouse、Honey Jar 各 80000 点/20 块；Birch 1、Oak 1 各 60000 点/15 块。使用同一个 PointCloudModel 出现、十指散开、提亮与回弹逻辑；树保留整体摇摆。
- 新增一键 Bake All Round Point Clouds 菜单。
- 关闭原树 LODGroup，避免视距变化重新启用已隐藏的原网格；Clear 时恢复原 LOD 状态。

## 渲染边界

这是带柔边的表面圆片点云，斜看呈椭圆，仍不是原生三维高斯椭球/排序渲染。Unity 编辑器与 visionOS 的边缘覆盖表现不同，最终柔边外观和五树同屏性能需真机复测。

官方依据：[PolySpatial 2.4.3 Materials](https://docs.unity3d.com/Packages/com.unity.polyspatial.visionos@2.4/manual/Materials.html)，URP Unlit Alpha Clipping 的 Threshold=0 在 visionOS 使用 alpha-to-coverage。

## 验证

Unity 6000.0.30f1 编译、全部模型和泡泡世界烘焙成功。首轮集成测试 66/66 通过，包含五模型网格/纹理、柔边 alpha、UV、材质配置、森林、完成小屋、泡泡及救援。

最终增加树模型几何尺度检查后 68/68 通过，0 失败、0 跳过。树叶透明卡片的空白边界不应当作为采样后高度的精确预期；尺度检查以排除单位转换错误为准。git diff --check 通过。
