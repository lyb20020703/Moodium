# 第二层泡泡世界修正结果

已完成离线坐标标准化、资源替换、Unity 烘焙和自动测试；尚无真机截图或真机验收结果。

| 第二层 | 点数／烘焙层 | 最终眼高 | 重配准残余角 | 相关评分 | 与次候选分差 |
| --- | --- | --- | --- | --- | --- |
| 自然 | 500000／20 | 0.369m | 0.5513° | 0.61175 | 0.27993 |
| 糖果 | 500000／20 | 0.560m | 0.4352° | 0.59461 | 0.19878 |

均满足角度 <1°、评分 >=0.55、分差 >=0.15。评分来自最终导出的 MSPC，非直接引用交接包评分。

## 修改范围

- `Assets/Resources/MoodiumBubbleWorld/NestedNature/` 和 `Candy/Nested/`：重新导出 points.bytes、圆点图集、frame.json，重烘焙 40 个点云层网格。保留原资源 GUID；全景图未修改。
- `BubbleWorldData.cs`：SourceFrameRotation 对四套世界均为 identity；删除无用矩阵转四元数方法。
- `BubbleWorldController.cs`：取消源坐标旋转、天空逆旋转及 .24/.79m 推测补偿；统一读取文件头实测眼高，仍保留面向用户的 yaw 和入场动画。保留同期嵌套入口、风和手部交互代码。
- `BubbleWorldBaker.cs`：增加仅烘焙两个第二层的菜单入口。
- `BubbleWorldTests.cs`：四世界规范坐标、眼高、每层首尾及内部共 17 个点中心一致性检查。
- `tools/convert_gaussian_to_bubble_world.py`：支持 frame JSON、正交变换校验、SHA-256/日期追溯以及导出最后的配准门槛。
- `tools/register_nested_world.py`：保留用户提供的全方向搜索和局部细化方法；增加 MSPC 输入、验收阈值、固定源坐标抽样与原始透明度 sidecar。图像缩放使用 Pillow BOX；原始糖果 PLY 重跑复现交接包 0.5848 评分及矩阵，验证该改动未改变该基准结果。

## 矩阵和抽样说明

初次复核直接把 MSPC 的不透明 alpha 当成源高斯透明度，又在旋转后的坐标重新分方向格抽样，造成糖果残余角变化。已修正为使用原始 opacity >0.35 筛选，并固定源方向格、点选择及排序。这样评分位置仍来自最终 MSPC，但微调前后不会随机换一批评分点。相关 sidecar 只用于临时导出验证，不进入 Unity Resources。

在固定抽样后，糖果原始文档矩阵即通过，不采用早期试验性糖果微调。自然原矩阵残余角 1.0097°，在用户明确允许离线微调后合并该残余变换，从原始 PLY 重新导出，复核为 0.5513°。原始 frame 与实际使用的 refined frame 都保存在 `second-layer-horizon/`；最终生产 `frame.json` 含原矩阵、微调记录及最终验证指标。

所有变换包含手系翻转，位置已直接烘进数据；运行时没有以四元数尝试表达镜像。眼高只记录真实原点离地距离，不额外平移点。自然雪坡／糖果地形仍保留自身起伏，不能把拟合地面的每一处斜坡都强行转平。

## 验证证据

- Unity 6000.0.30f1 两套 Nested 烘焙执行成功，退出码 0。
- 35 项 BubbleWorldTests／BubbleWorldAmbientWindTests 全通过，结果见 `second-layer-horizon/unity-tests.xml`。
- 首层网格、点数据、图集及四张全景共 50 个基线文件 SHA-256 无变化；见 `unchanged-baseline.json`。
- 两套圆点图集中心像素与导出颜色、alpha 一致性另行全量检查。
- `nature-registration.json`、`candy-registration.json` 是最终离线结果；其中原工具的 `mirror` 字段表示搜索基底符号，并非最终 T 的行列式。最终复核 T 均为 det=+1、接近 identity。
- `*-registration_aligned_vs_pano.png` 上半是最终点数据经最佳残余姿态投影（残余均小于 1°），下半是原全景；黑色为点中心投影空洞。它们不是 Unity 或头显截图。

## 复现

使用安装有 numpy、Pillow 的 Python。先导出到临时目录，通过后再复制三个生产文件并烘焙，不直接对 Assets 目录做失败风险尚未排除的导出。

```sh
python tools/convert_gaussian_to_bubble_world.py /Users/yibei/Downloads/b792f7bb25e24fbe792219ac5b03b953.ply /tmp/nature-export --frame-json docs/verification/second-layer-horizon/NestedNature.refined.frame.json --verify-panorama Assets/Resources/MoodiumBubbleWorld/NestedNature/panorama.png
python tools/convert_gaussian_to_bubble_world.py /Users/yibei/Downloads/b7dbd00c3878b09ecbf9d2f3e1851e7f.ply /tmp/candy-export --frame-json docs/verification/second-layer-horizon/Candy-Nested.frame.json --verify-panorama Assets/Resources/MoodiumBubbleWorld/Candy/Nested/panorama.png
```

Unity 菜单：Moodium → Bubble World → Bake Both Nested Worlds。只复制 points.bytes、point_colors_round.png、frame.json；不要复制 registration 的大数组或图到 Resources。

## 真机待验收

分别检查自然的松树、河道、落日，糖果的瀑布、拱桥、塔楼三个方向是否对齐、不镜像；检查小入口与展开世界一致。当前使用低机位真实眼高，站立用户的近地面视差仍需头显观察后决定是否重导出抬高视点。风／手部交互在运行时会有意移动点，严格核对静态地标时需与静态离线投影区分。第一层交互回归、实测帧率和双眼观感均不能由本次编辑器测试代替。
