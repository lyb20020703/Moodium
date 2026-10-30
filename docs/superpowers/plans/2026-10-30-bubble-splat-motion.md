# 点云运动实施计划

状态：用户已明确授权不等待真机验收，完成引擎内全部实施。代码、Graph、两个 Sub Graph、80 层重烘焙、数学与驱动测试、GPU 对照渲染均已完成；下列步骤中涉及真机录屏、真实追踪及帧率的部分仍待设备验证。最终记录：`docs/validation/bubble-motion/README.md`。

1. 保存现有按钮修改；确认 Unity、PolySpatial、Shader Graph 版本及支持节点。读取参考项目的风和触碰实现，核对本地 Graph 序列化结构。
2. 创建零位移静态 Shader Graph、资源材质与运行时加载分支，日志输出实际 shader 名；缺失材质退回 Unlit 并警告。测试 _BaseColor 淡出及圆点透明度。执行 Unity 编译和 PolySpatial 转换检查。
3. 第一阶段真机验收：四套世界静态显示、边缘和排序一致，收集 shader 日志。确认后继续。
4. 实现 seed/sway、bounds 扩展和 CPU 参考数学；添加烘焙和数学边界测试。四套世界重新烘焙，校验顶点数、UV0、UV1、bounds 和 GUID。
5. 构建 HandDisturb 与 SplatHash Sub Graph，连接风、漂浮、自身浮动到 Vertex Position；诊断模式 Boost=8、DebugTint 开启。真机确认可见位移后继续。
6. 实现 BubbleSplatMotion 的阶段状态、局部坐标转换和变化阈值更新；正常幅度真机录制自然/糖果各层的 20 秒效果，核实近远点和方片形变。
7. 接入双手张开、捏合冷却、失踪延迟与强度平滑；用 Boost=3 和调试色检查近处/远处响应，再恢复正式参数。
8. 删除 AmbientWind、HandRipple、未使用的 UpdateQuietLayerDrift 与对应旧测试；保留 CacheQuietLayerPose/RestoreQuietLayerPose。移除所有挂载和生命周期调用，确保外层 HandingOff 驱动衰减、内层独立。
9. 运行资源、数学、驱动、新烘焙、BubbleWorldTests 与嵌套交接测试。真机检查回家捏合不会触发散开、切换与退出无残留。
10. 记录静止和双手扰动帧率，与同设备旧版对比；交付阶段日志、截图/视频、参数和变更记录。没有实测的项目明确标记未验证。
