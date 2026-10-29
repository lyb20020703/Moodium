# UI Refresh 接入设计

依据用户提供的 Moodium-UIRefresh.zip README、任务说明、tokens 和四张预览。

## 现有入口
- FloatingSqueezeProgress：Canvas + Image 横条；CandyWorldManager.BindSqueezeVideoWhenSoftAppears 绑定 Soft，SetProgress 同步能量。改为 SpriteRenderer 40 点环、菱形和 TMP 数字；沿用绑定/生命周期，补传项目字体及按压唤醒事件。
- SensoryBagCalibrator：边/角/框/高度共享 SensoryBagCalibrationHandle 指针路由；当前旋转实际是两个 +/-5°文字按钮，RotateFrame 修改 anchor 围绕框中心旋转。新增 Rotate handle kind 复用该路径，拖动基于固定起始平面计算角度，复用旋转实现，加入3°归零吸附。边/角/升降/确认/重置保留。
- MoodiumAppFlowController.OpenSelectionSettings/ApplySelectionSettings：文字按钮面板；立即写 PlayerPrefs/ownership/session language 后重建选关。WorldCarouselSelectionController 也有设置入口。两处替换齿轮，用同一面板组件；重建选关时保留独立面板以完成切换动画，设置仍立即应用。
- OpeningVideoLanguageChoice/HardwareChoice：Canvas Image 胶囊及问题文本，OpeningVideoSpatialButton + SpatialUIInputManager 路由，ChoiceGate 控制切换。替换为 SpriteRenderer，保持已有选择回调/门控；按压反馈增加0.3s显示，编辑器鼠标复用同一按钮回调。

## 视觉和边界
51 张素材按文件名导入，PPU=1857.142857、默认 sprite 材质、原生 hover、每处 VisionOSSortingGroup。集中布局常量和动态文本尺寸。保持手腕 UI、能量/奖励阈值、视频与持久化逻辑。保留工作区现有 Ivy/风/朝向调查改动，不提交。

## 验证
Unity 编译；素材/布局/显示状态/校准旋转/设置/开机回归 EditMode。编辑器离屏相机渲染实际组件输出37%、100%、旋转、中英设置截图；与设备透明度、原生hover、手势和性能分开报告。
