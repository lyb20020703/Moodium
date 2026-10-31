# Agent 任务：泡泡世界里的三个交互玩法

## 背景与硬性约束

现在泡泡世界里的点云只能看，不能互动。之前试过给点加扰动（GPU 运动着色器），每个圆点都会变成方块，所以**这三个玩法都不移动、不改形状任何一个点**。

- 点云只允许两种改动：整层换材质（染色），或者整层变换（已有的呼吸缩放）。
- 互动的主体都是独立的物体：粒子、小模型、风铃、笔迹。
- 不要启用 `BubbleSplatMaterial.Create(..., motion: true)`。

**分两步交付**：

| 阶段 | 玩法 | 在哪 |
|---|---|---|
| 第一步（最小可用） | A. 光的涟漪 | 四个泡泡世界（两个世界 × 两层） |
| 第一步（最小可用） | B. 接住飘落的东西 | 两个第二层：薄荷巧克力、雪原 |
| 第二步（重点） | C. 心愿风铃 | 四个泡泡世界 |

**依赖**：已接入的"泡泡世界讲解"补丁，用到其中的 `BubbleWorldNarrator`（2D 讲解）和 `BubbleWorldController.LayerRevealed`。

## 素材放到工程

| 包内 | 工程里 |
|---|---|
| `WishChime/Models/WishChime_Candy.fbx`、`WishChime_Nature.fbx` | `Assets/Resources/MoodiumBubblePlay/` |
| `Catch/Models/CatchBits.fbx`（`Snowflake`、`ChocoChip`、`MintShard` 三个物体） | `Assets/Resources/MoodiumBubblePlay/` |
| `Audio/*.wav`（16 个） | `Assets/Resources/MoodiumBubblePlay/Audio/` |
| `Voice/zh/*.wav`、`Voice/en/*.wav`（各 9 句） | `Assets/Resources/MoodiumVoice/zh/`、`en/` |

- 模型单位是米，Y 轴朝上。
- 运行时自己建 Unlit 材质（参考 `BubbleWorldData.CreateUnlit`），颜色见各节，不用 FBX 自带的材质。
- 风铃出现的音效，直接用已有的生长音效 `MoodiumGrowth/grow_candy`、`grow_nature`。

### 引导语音（Moodi 的 Cute Spirit 音色，中英各一套）

| 编号 | 中文 | English | 什么时候说 |
|---|---|---|---|
| B1 | 捏一下手指，看看会发生什么～ | Pinch your fingers, and see what happens! | 每次进入世界，第一层讲解结束后 2 秒，说一次 |
| B2c | 伸出手掌，接住飘下来的巧克力碎吧～ | Hold out your palm, and catch the falling chocolate bits! | 薄荷巧克力层讲解结束后 2 秒 |
| B2n | 伸出手掌，接住飘落的雪花吧～ | Hold out your palm, and catch a falling snowflake! | 雪原层讲解结束后 2 秒 |
| B3 | 要不要写下一件想放下的事，或者一个小心愿？ | Would you like to write down something you want to let go of, or a little wish? | 空白风铃出现时 |
| B4 | 捏住手指，就可以在纸条上写字啦。 | Pinch your fingers to write on the paper. | 第一次进入书写状态 |
| B5 | 写好了吗？把它托起来，轻轻放飞吧。 | All done? Hold it up, and gently let it go. | 写完后 3 秒没动作（每只风铃一次） |
| B6 | 它会替你在这里保管。 | It will keep it safe for you, right here. | 风铃挂好时 |
| B7 | 你的风铃还在这里等你呢。 | Your wind chimes are still here, waiting for you. | 进入某一层、这一层已经挂着风铃时（每次进入世界一次） |
| B8 | 没关系，重新写就好。 | That's okay, just write it again. | 重写时（每只风铃一次） |

- 这些语音通过 `BubbleWorldNarrator` 播放（2D，因为泡泡里看不到 Moodi）。请给它加一个 `SayLine(string id)`：排在当前讲解后面播放，同一时刻只说一句。
- **不要**把它们加进 `MoodiVoice.AllIds`，否则 `MoodiVoiceTests` 里的数量检查会失败。

---

## A. 光的涟漪

**用户感受**：捏一下手指，一圈光从身边向远处荡开，经过的地方依次染上一层暖色再褪回去，伴随一段渐行渐远的音阶。

**触发**
- 任意一只手：拇指尖和食指尖距离从 > 2.5 cm 变成 < 1.5 cm 的那一帧（XR Hands）。
- 只在 `Phase.Inside` 时生效，两次之间冷却 2.5 秒。
- 以下情况忽略，避免和别的捏手势冲突：捏的位置在任意风铃 15 cm 内、在回家泡泡或下一层泡泡 15 cm 内、正在写字。

**做法**（新组件 `BubbleLightRipple`，第一层和第二层的控制器各挂一个）
1. 从 `m_PointMaterial` 复制两份"染色材质"：
   - 峰值色：糖果 `(1, .86, .93)`，自然 `(1, .95, .82)`；
   - 回落色：糖果 `(1, .93, .97)`，自然 `(1, .98, .92)`；
   - alpha 都跟原材质一致。
   - 原材质的 alpha 被改时（`SetAlpha`，比如走远变淡），这两份也要同步。
2. 波从观看者向外传播，速度 6 m/s。第 i 层的半径 = `m_LayerRadii[i] × m_WorldRoot.lossyScale.x`。
3. 波到达某层时，把这一层的 `sharedMaterial` 依次换成：峰值色 0.18 s → 回落色 0.25 s → 原材质。**只换材质，不逐帧改参数。**
4. 前 8 层到达时，各在该半径上调用一次已有的 `Sparkle(radius)`。
5. 播放 `ripple_candy.wav` / `ripple_nature.wav`（2D，音量 0.6）。

**性能**：同一帧最多换 3 层材质；全部过程不改任何材质参数。

---

## B. 接住飘落的东西（只在两个第二层）

**用户感受**：
- 雪原里雪花慢慢飘落。伸出手掌（掌心朝上）就能接住，雪花在掌心停一会儿，慢慢化掉。
- 薄荷巧克力层飘的是巧克力豆和薄荷糖碎，接住后在掌心停 3 秒，化成几点闪光。

**做法**（新组件 `BubbleCatchFall`）
- **对象池**：36 个物体，不用粒子系统，方便按距离判断有没有接住。
  - 雪原：全是 `Snowflake`，颜色 `#F2FBFF`。
  - 薄荷巧克力：`ChocoChip`（`#5A3524`）和 `MintShard`（`#BFF2DD`，alpha 0.85）各一半。
- **生成**：以头部为中心、水平半径 1.2 m 内，从头顶上方 1.0 m 处生成。
  - 下落速度 0.12–0.22 m/s，左右轻摆（幅度 3 cm、周期 2–3 s），缓慢自转。
  - 落到头部以下 1.2 m 时回收。
- **接住的条件**：
  - 掌心朝上：用已有的 `MoodiHandCompanion.PalmFacing`，点积 up > 0.6；
  - 物体在掌心上方 7 cm 以内。
  - 满足后，物体贴在掌心，跟着手动，播放 `catch_snow_0..2` / `catch_choco_0..2` 中随机一个（3D，音量 0.5）。
  - 掌心最多同时 5 个。
- **之后**：
  - 雪花：2.5 秒内缩小到 0，然后回收。
  - 巧克力：停 3 秒，调用 `SpawnSoftSqueezeFeedback`，然后回收。
  - 手掌翻过来或手丢失时，掌心的东西继续往下落。
- 只在 `Phase.Inside` 时运行；离开泡泡时全部回收。

---

## C. 心愿风铃

交互原型：https://claude.ai/artifact/5zJdftVUrTQUKbpqimDCgx （需要账号主人分享后才能打开）。打不开的话，以本文档和 `WishChime/Prototype/` 里的页面为准。

### 模型

整只风铃高约 20 cm，原点在吊环（摆动支点）。零件按名字查找：

| 名字 | 说明 | 糖果 | 自然 |
|---|---|---|---|
| `Ring`、`String_Top`、`Clapper_String`、`String_Strip` | 吊环和细绳 | `#E98FB5` | `#7A5A3A` |
| `Bell` | 玻璃铃罩（半透明） | `#FFB3D1`，alpha 0.35 | `#E8FFF3`，alpha 0.25 |
| `Glow` | 铃罩里的光（糖星 / 萤火虫） | `#FFE7B0`，亮 | `#D9FF8A`，亮 |
| `Clapper` | 玻璃铃舌 | 白色，alpha 0.6 | 同左 |
| `Deco_*` | 铃罩上的糖粒 / 小叶 | 彩色 | `#6FCF8E` |
| `Strip` | 纸条，3.4 × 11 cm。UV 0–1，u 横向，v 向上，朝向观看者的一面是正面 | `#FFE6F0` | `#C9A574` |
| `Leaf_Deco` | 纸条角落的叶子（只有自然） | — | `#6FCF8E` |

笔迹颜色：糖果 `#C93D78`、外发光 `#FF78B4`（alpha 0.35）；自然 `#3E2A16`、外发光 `#FFE98C`（alpha 0.35）。

### 流程

1. **出现**：某一层讲解结束后 12 秒（用户不在写字、不在走动），在观看者面前 0.55 m、头部下方 0.25 m 处长出一只空白风铃。
   - 0.9 秒内从 0 长大，播放生长音效，同时说 B3。
   - 每层每次只出一只空白风铃。放飞后如果还在这一层，20 秒后再出一只。
2. **进入书写**：在纸条 8 cm 内捏一下，或者看着纸条点一下（给纸条加 `MoodiumSpatialButton` 碰撞体）。
   - 风铃移到眼前 0.4 m，纸条放大 2.5 倍（约 8.5 × 27.5 cm），正对观看者，停止摆动。
   - 第一次进入时说 B4。
3. **书写**：
   - 笔尖 = 拇指尖和食指尖的中点。捏住（< 1.5 cm）就是落笔，松开（> 2.5 cm）就是抬笔，用这个滞后区间防止抖动。
   - 笔尖在纸条平面 3 cm 以内、并且在纸条范围内时，才记录笔迹点。投影到纸条上，存成 UV 坐标。
   - 相邻两点在纸条上至少相距 1.5 mm；做简单的指数平滑（系数 0.5）。
   - 笔尖离纸条 6 cm 以内时，显示一个 4 mm 的发光小球当笔尖光标。
   - 写字时循环播放 `chime_write_loop.wav`，音量跟笔尖速度走，停笔就静音。
   - **笔迹渲染**：在纸条本地坐标里生成"带状网格"，宽 1.6 mm（按纸条原始大小），离纸面 0.4 mm。底下再铺一条宽 3 倍的外发光带。
     - 正在写的那一笔，每帧重建（不超过 300 个顶点）；写完的笔画合并成一个网格。
     - **不要用贴图绘制**，在 visionOS 上逐帧更新贴图很贵。
   - 每只风铃最多 4000 个笔迹点。
4. **重写**：纸条左边浮一个小的"重写"按钮，或者快速晃动纸条（角速度超过阈值）。
   - 笔迹在 0.5 秒内淡出后清空。说 B8（每只风铃一次）。
5. **放飞**（至少写了一笔才可以）：
   - 可靠方式：纸条右边浮一个"放飞"按钮。
   - 手势方式：手掌朝上放在铃罩下方 12 cm 以内，然后 1 秒内向上抬超过 10 cm，或者张开手。
   - 写完后 3 秒没有动作，说 B5（每只风铃一次）。
6. **飞走**：
   - 纸条缩回原大小。风铃在 2.6 秒内缓慢升到目标位置，边升边摆。
   - 播放 `chime_release.wav`，到达后播放一次 `chime_ring_*`，说 B6。
   - **目标位置**：以进入点为圆心、水平半径 0.8–1.6 m、高度在头部上方 0.3–0.8 m 之间，选一个空位。离其他风铃至少 0.35 m，并且不挡住正前方 30° 以内的视线。
7. **挂着**：
   - 一直轻轻摆动，幅度 2°，周期约 2.4 s。
   - 每隔 25–60 秒（随机）自己响一次，3D 声音，音量 0.25。
   - 用指尖碰到铃罩或纸条：摆一下、响一声。

### 永久保存

- 文件：`Application.persistentDataPath/wish_chimes.json`。每次放飞后写入，先写临时文件再改名，防止写一半坏掉。
- 格式（以后的"心愿日记"直接读这份数据）：

```json
{
  "version": 1,
  "chimes": [
    {
      "id": "8c0b…",
      "world": "candy",
      "layer": 1,
      "folder": "MoodiumBubbleWorld/Candy/Nested/",
      "createdAt": "2026-10-31T01:20:00+08:00",
      "position": [0.42, 0.38, -0.71],
      "yaw": 35.0,
      "strokes": [[[0.31, 0.12], [0.33, 0.15]], [[0.52, 0.40], [0.50, 0.47]]]
    }
  ]
}
```

- `position` 存 `m_WorldRoot` 的**本地坐标**（除以世界缩放），这样无论用户站在哪里进入，风铃都挂在这个世界里的同一个地方。
- 进入某一层（`LayerRevealed`）时，读取并挂出这一层的全部风铃。如果有，讲解结束后说 B7。
- 同一层超过 40 只时，只完整显示离用户最近的 40 只，其余只显示铃罩里的光点。
- 数据只存在设备本地，不上传。

---

## 测试（EditMode）

- **涟漪**：波到达时间的计算（半径 / 速度）；捏在风铃附近时不触发；冷却时间生效。
- **接住**：`PalmFacing` 点积 > 0.6 且距离 < 7 cm 才算接住；掌心最多 5 个。
- **风铃**：
  - 笔迹点间距和平滑；
  - 不足一笔时不能放飞；
  - 选位置时离其他风铃 ≥ 0.35 m；
  - 保存后再读取，位置、笔迹完全一致（往返测试）；
  - 文件损坏时不崩溃，按空列表处理。
- **素材**：两个风铃模型、`CatchBits`、16 个音效、18 句语音都能从 Resources 加载；模型里的零件名字都存在。
- 原有的 `BubbleWorld*`、`MoodiVoiceTests`、`BubbleWorldNarratorTests` 仍然通过。

## 真机验收

1. **涟漪**：四个泡泡世界里各捏一次，光圈由近到远，点仍然是圆的，帧率不掉。
2. **接住**：两个第二层，掌心朝上能接住，雪花会化掉，巧克力会变成闪光。
3. **风铃**：
   - 写两个字、重写一次、放飞；
   - 离开泡泡世界再进来，风铃还在原处；
   - 重启 App 再进来，风铃还在。
4. **手势不打架**：写字时捏手指不会触发涟漪。
5. 把各阶段的帧耗时记录写进 `docs/verification/2026-10-31-bubble-play.md`。

## 注意

- 第一步（A + B）可以先单独提交，第二步（C）再提交。
- 设计上走不通的地方先报告，不要自己改玩法。
- 不要改点云数据、点大小和点云着色器。
