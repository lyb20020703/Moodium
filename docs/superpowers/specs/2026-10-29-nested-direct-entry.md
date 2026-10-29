# 第二层入口连续交接规格

入口是预加载的第二层实例本身。第一层进入 Inside 后启动第二层 Build，至少等 10 秒且核心层已就绪才显示半径 0.08 m 的 Portal。点击直接从当前姿态进入 Approach，共用第一层的 Approach、Envelop、Reveal 动画。

外层保持 HandingOff 状态，内层半径从 1 m 到 4 m 时外层点云从不透明变为透明。内层天空在 Envelop 阶段淡入，世界半径为 SkyMaxRadius × 0.97，Reveal 时外层天空关闭，内层 Revealed 后释放外层资源。内层展开后恢复完整天空半径。

外层负责转发内层 Left、Wandered、HomeRequested。内层 Touched 只启动外层交接，不通知房间管理器。内层回房间使用最初的外层入口位置；再次召唤重新加载外层。手腕退出交接时外层用 0.4 秒淡出，内层沿既有 Return 路径离开。

保留现有 SourceFrameRotation 与地面定位修改，独立的地平线资产转换不包含在本次交接修改中。
