# 实施计划

1. 保留工作区现有修改，将外层资源清理与控制器生命周期分开。
2. 添加独立内层控制器，在外层 Revealed 后预热；删除预览泡泡和重新 Begin 的旧入口。
3. 添加 Portal 与 HandingOff 阶段，共用 Approach 之后的动画，设置连续天空交接。
4. 转发内层事件、保留原房间入口位置，处理交接退出和再次召唤。
5. 添加位置/半径连续性与淡出测试，运行 BubbleWorldTests、BubbleWorldNestedEntryTests 和 BubbleWorldAmbientWindTests。
6. 真机分别检查自然/糖果的连续性、交接帧率、回房间与手腕退出。
