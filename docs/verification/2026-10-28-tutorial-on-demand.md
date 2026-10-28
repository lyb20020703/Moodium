# 玩法教学按需打开：接入与验证

依据 `Moodium-TutorialOnDemand.zip` 中的 README 接入现有工程。

## 已接入

- 应用包内代码补丁：新增 GuideHelpController、HelpPuck 和 GuideHelpTests，修改 WorldGuideCards、SqueezeTableView、ModeButtonBar、SqueezeModeController、CandyWorldManager。
- 进入世界时取消自动弹教学卡，首次求救结束后问号泛光；通过问号打开桌面教学卡，支持关闭、翻页和模式联动。
- 满能量时关闭教学卡，救援流程后延迟 11 秒显示新内容圆点和 6 秒文字提示；无救援故事时延迟 2.5 秒。
- 复制 12 张新增 PNG，并通过 Unity 执行 SqueezeModesSetup.Setup 设置桌面贴图；教学卡贴图由已有 AssetPostprocessor 导入。
- 增加 `Moodium/Validate Tutorial On Demand` 菜单，可重复执行资源设置与相关回归测试。

## 验证结果

- Unity 6000.0.30f1，当前平台 VisionOS：编译完成，编译错误 0。
- EditMode：36 项通过，0 失败。覆盖 GuideHelpTests、WorldGuideCardsTests、WorldGuideLifecycleTests、SqueezeTableViewTests、SqueezeModeControllerTests。
- 新增 GuideHelpTests 的 8 项全部通过，覆盖按钮/卡片位置、首次泛光、提示淡入淡出、页面映射、首次访问记录、卡片开关及新增资源。
- `git diff --check` 通过。
- 结果 XML：`/var/folders/r0/92tsbtv569lfgkrwytf1z9rr0000gn/T/Moodium-TutorialOnDemand-EditMode.xml`。

## 尚需真机确认

糖果与自然世界各走一遍：首次与再次进入、触碰/注视捏合问号、桌面卡片位置与拖动翻页、模式联动、100% 提示时序，以及退出世界时 UI 清理。编辑器测试不能替代这些空间交互检查。

接入前已有的自然世界点云、字体和工程设置等本地修改保留。
