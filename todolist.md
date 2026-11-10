# Tetris 项目改进待办清单

> [!note] **说明**
> 这是我让 Pi agent 分析项目之后，整理出来的改进建议清单。  
> 生成时间：2026-11　基于对全量源码（`owlgame/`，约 700 行）的静态审查 + 两项实测探针验证
> **进度**：第 1 轮（1 / 3 / 6 / 14 / 16）、第 2 轮（2 / 5）、第 3 轮（4 / 7 / 8 / 11）、第 4 轮（9）、第 5 轮（12 / 13 动画部分 / 15）已完成，验证见 `tests/README.md`（`bash tests/run.sh` 一键回归，44 条断言）
> **全部完成**：16 项中 15 项已实现（第 10 项 LICENSE 已采用 MIT）；第 13 项的音效部分未做（缺音频资源，原作者版也无音效）
> **P4 主体完成**：17~21 已落地 —— 自制测试框架 + 57 个用例（`bash tests/run.sh`）+ GitHub Actions（`--skip-ui`）；顺带修掉一个从第二轮起就存在的消行索引 bug。
> **P4 遗留**：第 22 项（6 处边界未真正覆盖）、第 23 项（public 成员覆盖率对照），见下文「实测结果」
> **P4 收尾完成**：第 22 项（6 处边界）与第 23 项（public 成员覆盖率对照）已完成，用例数 57 → **71**，并顺手删掉一个无调用方的死方法。
> **P5 已完成**：24~32 UI 设计现代化已按 **D7-B-lite**（保持棋盘几何、现代化外围）落地。
> **第 33 项**：全面测试发现并修掉两处状态机缺陷（动画期间的停止/重玩/结束/帮助），用例 72 → 76，均做了回退验证。
> **第 9 轮（对外门面）**：34 —— README 补 CI/协议/运行环境三个徽章 + 「界面」章节三张实拍截图 + 目录结构补齐 `tests/`、`.github/`、`img/`；顺带修掉 `owlnan` 拼写（README 与界面署名各一处）。新增 `tests/build-release.sh` 负责回写被跟踪的 exe。
> 配套文档：[`AGENTS.md`](AGENTS.md)（构建/编码/验证规范）　[`README.md`](README.md)（项目说明）

**图例**：`[ ]` 待办　`[x]` 已完成　`[!]` 需要用户拍板　`💡` 改进建议（非缺陷）

---

## P0 —— 已实测确认的缺陷，优先修复

### [x] 1. `Random` 种子重复：开局「当前块」与「下一个块」完全相同

- **现象**：`gameController` 构造函数中相邻创建 `now` 与 `wait` 两个 `block`，两者形状与颜色完全一致（预览框里看到的下一个块就是当前的块）。
- **实测**（探针输出）：

  ```csharp
  now  shape = .....|.##..|..#..|..#..|.....| color=Yellow
  wait shape = .....|.##..|..#..|..#..|.....| color=Yellow
  shapes identical = True     colors identical = True
  fresh block 0..4 = .....|.##..|..##.|.....|.....|   ← 连续 5 个实例全同
  ```

- **根因**：`block.cs` 中 `private Random rander = new Random();` —— 每个 `block` 实例各建一个 `Random`；.NET Framework 无参构造以 `Environment.TickCount`（约 15ms 精度）作种子，15ms 内创建的多个实例序列相同。
- **建议方案**：改为**静态共享**的随机源，并把取形状/取颜色拆成两次独立取值（避免颜色与形状强相关）：

  ```csharp
  private static readonly Random Rander = new Random();
  private static readonly object RanderLock = new object();
  ```

  取值时加锁（`Random` 非线程安全），或直接换成 `RNGCryptoServiceProvider` / `System.Security.Cryptography.RandomNumberGenerator` 取索引。

- **附带问题**：`transpose()` 每次旋转都 `new block(...)` 只为复制矩阵，白白消耗随机序列 —— 改为浅拷贝矩阵即可。
- **验证**：重跑探针，断言 `now` 与 `wait` 的形状/颜色**不相同**；连续创建 5 个 `block` 断言形状不全部相同。
- **影响面**：`owlgame/block.cs`、`owlgame/gamecontroller.cs`（`transpose`）
- **预估**：0.5 小时

### [x] 2. 每帧新建 GDI 对象 + 全棋盘重绘（性能）

- **现象**：闪烁修复把局部重绘改成了整块 `view.Invalidate()`，每次按键都全清 + 全画 220×388 的棋盘；同时 `draw()` / `drawwait()` 每次都 `new Pen` + `new SolidBrush` 再 `Dispose`。
- **根因**：为绕开「脏矩形偏移算错」的历史 bug 直接退化为全量重绘；GDI 对象未做缓存。
- **建议方案**：
  1. 把 `Pen` / `SolidBrush`（棋盘背景、方块、墙、当前块色、预览框色）提到 `gameController` 字段，构造时创建、释放时销毁；
  2. 把重绘范围收窄回脏矩形 —— **必须修正偏移公式**（历史 bug：`(j + now.curx - 4) * bw` 多减了 4 格，正确应为 `(j + now.curx) * bw`），并只重绘「旧位置 ∪ 新位置」的并集矩形；
  3. 保留 `Opaque` 契约：`draw()` / `drawwait()` 仍需先铺满背景（脏矩形版本只需铺脏区）。
- **风险**：脏矩形算错会导致**残影**，比闪烁更难察觉 —— 必须配像素级回归探针（见验证方式）。
- **验证**：
  - 像素探针断言棋盘在按键后**不存在残影**（对比操作前后整幅 bitmap 的差异像素数 = 预期变化区域）；
  - 统计 `WM_PAINT` 次数与 `GetUpdateRect` 覆盖率（修复前基线：整屏覆盖）；
  - `WM_ERASEBKGND` 计数必须保持 ≤ 1（不能因改回局部重绘而回升）。
- **影响面**：`owlgame/gamecontroller.cs`（`draw` / `drawwait` / `Invalidateblock`）
- **预估**：2~3 小时

### [x] 3. 反射调用静默吞掉异常

- **现象**：`EnableDoubleBuffer()` 与 `ApplyFlickerFix()` 均为 `catch (Exception) { }`。一旦反射失败（框架版本差异、控件被裁剪），**闪烁会悄悄回来**，没有任何提示。
- **建议方案**：至少 `catch (Exception ex)` 后 `Debug.WriteLine(ex)` / `Trace.WriteLine`；更稳妥的做法是加一个启动自检开关（环境变量或 `DEBUG` 编译符号），失败时弹窗提示。
- **验证**：临时改一个属性名制造反射失败，确认有可见日志/提示。
- **预估**：0.5 小时

---

## P1 —— 架构与可维护性

### [x] 4. 逻辑层与绘制层解耦，让核心逻辑可单元测试

- **现状**：`gameController` 直接持有 `Panel view` 并在 `draw()` 里做 GDI 绘制 —— 无法脱离 WinForms 单测，这也是当初只能靠「临时探针 exe」验证的原因。
- **建议方案**（分两步，别一次做太狠）：
  - **第一步**：把棋盘模型抽成 `pure GameBoard`（只管 `map` / `now` / `wait` / 落子 / 消行 / 判定），`gameController` 退化为「模型 + 绘制适配」；
  - **第二步**：为 `GameBoard` 写真正的单元测试（可用 `nunit`/`xunit`，或最朴素的自写断言程序），覆盖：落子边界、消行（单行/连续多行/隔行）、旋转后越界、失败判定。
- **收益**：消行、越界、失败判定这类逻辑可以脱离 GUI 快速迭代（本次评估中「消行逻辑是否只消一行」就是靠 Python 复刻才验证的，本该有 C# 单测）。
- **预估**：半天 ~ 1 天

### [x] 5. 建立回归验证脚本（替代一次性探针）

- **现状**：两个探针源码已固化到 `tests/`（`Round1Regression.cs` / `PixelSmoke.cs`），但还没有一键脚本；探针 exe 仍需要手工编译。
- **建议方案**：在 `tests/` 下固化：
  - `tests/board.tests.cs` —— 纯逻辑断言（配合第 4 项）；
  - `tests/pixel.probe.cs` —— 像素级探针（控件文本、预览框边框色、棋盘残影检测）；
  - `tests/run.sh` —— 一键：拷贝源码到纯 ASCII 构建区 → 编译 → 跑断言 → 输出报告。
- **注意**：探针 `.cs` 文件**必须带 UTF-8 BOM**，否则 `csc` 按 ANSI(936) 读取会报一堆无意义的 `CS1513 / CS1519` 语法错误；且 `*.cs` 通配编译前要清掉探针文件，否则 `CS0017` 多入口点。
- **预估**：半天

### [x] 6. 外部链接未校验 scheme

- **现象**：`mainForm.myweb_LinkClicked` 直接 `Process.Start(url)`，`url` 来自控件 `Tag`（设计器里写死为外链，但无任何校验）。若 `Tag` 被换成本地路径或 `file://`，理论上可拉起本地程序。
- **建议方案**：只允许 `http` / `https`，其余忽略并记日志。
- **预估**：15 分钟

---

## P2 —— 代码风格与工程卫生

### [x] 7. 命名现代化

- `gameController` / `mainForm` / `block` → `GameController` / `MainForm` / `Block`（C# 规范：大驼峰）
- `rander` → `rander` 拼写错误，应为 `random` / `Rander`
- 常量 `No`（种数）、`size`（矩阵维数）→ `ShapeCount` / `MatrixSize`
- 成员 `map` / `now` / `wait` / `bw` / `bh` → `board` / `current` / `next` / `cellWidth` / `cellHeight`
- **注意**：改名会牵动 `mainForm.cs` / `mainForm.Designer.cs`（`mainForm` 是窗体类名，改名要同步 `.Designer.cs` 与 `.resx` 里的 `mainForm` 资源前缀）
- **预估**：1 小时（可与第 4 项一并做）

### [x] 8. 消除 magic number

- 棋盘控件尺寸 `220×388`、预览框 `79×53`、预览定位 `box.Width/4` / `box.Height/9`、列数 `20`、颜色 `AliceBlue` / `Gray` 全部硬编码。
- **建议方案**：抽为 `const` / `static readonly`，或从布局数据推导（如预览框位置按棋盘中心对齐，而非硬编码系数）。
- **预估**：1 小时

### [x] 9. 清理仓库中误跟踪的构建产物与噪声

- 当前被跟踪：`owlgame/bin/Debug/owlgame.vshost.exe`、`owlgame/bin/Debug/elsfk(by .NET).rar`、`owlgame/obj/**` 编译产物。
- **建议方案**：`git rm --cached` 移出版本控制 + 补 `.gitignore`（注意 `.gitattributes` 里是 `* -text`，行尾不会被规范化，移除产物不影响行尾一致性）。
- **注意**：`owlgame/bin/Debug/owlgame.exe` 与 `owlgame.pdb` **是有意跟踪**的（老项目惯例，保证 clone 即可运行），不要误删。
- **预估**：30 分钟

### [x] 10. 补 LICENSE

- 仓库无许可证文件。若要公开到 gitee/github，建议补一个 MIT（与 `CS_StudyNotes` 一致）或明确标注「仅供学习交流」。
- **预估**：10 分钟

### [x] 11. 清理冗余代码

- `clearLine()` 消行后调用 `initMap()` 全图重扫 —— 功能正确但纯属冗余（墙的初始化本就不该在消行后重做）。
- `draw()` / `drawwait()` 中 `Pen` / `SolidBrush` 的手工 `Dispose`（若做了第 2 项改为 `using` 或字段持有）。
- **预估**：30 分钟

---

## P3 —— 功能补全（可选，视目标而定）

### [x] 12. 计分与等级

- 无任何计分。建议：按消行数计分（1/2/3/4 行分别给不同分值）、连消加倍，每消除 N 行升一级并加快下落速度。
- **预估**：2 小时

### [x] 13. 消行动画与音效

- 当前消行是「瞬间消失 + 整体下移」，无过渡。最小实现：在 `clearLine()` 前后插一个短定时器状态，先把待消行刷成高亮色停留 100~200ms，再真正移除。
- **预估**：半天

### [x] 14. 方块颜色按形状固定

- 现在每个方块**随机取色**（红/黄/蓝三色之一），不符合俄罗斯方块惯例（同形状同色）。
- **建议方案**：把 7 种形状与固定颜色一一对应（`blockbox` 已按索引组织，改造成本低）。
- **预估**：30 分钟

### [x] 15. 下落速度与手感

- `gametimer.Interval = 1000`（1 秒/格），偏慢；按键 `↓` 一次下移 2 格。
- **建议方案**：随等级动态调整间隔，或改为「按时间累计下落距离」的模型（帧率无关）。
- **预估**：1 小时

### [x] 16. Game Over 后无法直接重开

- 失败分支里 `buttonstart.Enabled = false`，用户只能点「重玩」。
- **建议方案**：失败时把「开始」按钮恢复可用并改文案，或让「重玩」成为默认焦点。
- **预估**：10 分钟

---

## P4 —— 单元测试（2026-11 新增）

**背景**：第 4 项把逻辑拆成了不依赖 WinForms 的 `GameBoard`，第 5 项建了 `tests/run.sh` 一键回归，但两者都还不是「单元测试」：

| 现状 | 缺口 |
| --- | --- |
| 5 个探针各自是带 `Main` 的控制台程序 | 没有测试框架，没有用例发现与筛选，一个断言失败后续仍继续跑 |
| 断言散落在 `Program.cs` 风格的代码里 | 没有 `[Test]` / `[TestCase]` 语义，无法按被测类组织、无法参数化 |
| 只有 44 条断言 | `GameBoard` / `Block` 大量边界分支没有覆盖（旋转、四边碰撞、多重消行组合） |
| 结果靠肉眼读 `out_*.txt` | 没有标准报告（TRX / JUnit XML），无法接 CI 看板 |
| UI 与逻辑混在同一个可执行文件里 | UI 测试需要消息循环，无法单独跳过 |

### [x] 17. 选定测试框架（决策 D6）

**约束（先确认再选型，不要凭印象）**：

- 编译器固定为 `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`（**C# 5**）；
- 本机没有 .NET SDK / MSBuild / NuGet CLI，第三方包只能手动下载 `.nupkg`（本质是 zip）解压取 dll；
- 目标框架实际是 .NET Framework 4.0 运行时（csc 默认引用 4.0 程序集）。

**候选**：

| 方案 | 可行性 | 代价 |
| --- | --- | --- |
| A. NUnit **3.x** | ⚠️ 需核实：NUnit 3 要求 .NET 4.5+，与 4.0 运行时可能不兼容 | dll 约 300 KB 需入库 |
| B. NUnit **2.6.x** | ✅ 支持 .NET 2.0+，与 4.0 兼容性最好 | 老版本，断言 API 较旧 |
| C. xUnit | ❌ 要求 .NET 4.5.2+ | — |
| D. 自制极简框架（`[Test]` 特性 + 反射运行器 + `Assert` 静态类） | ✅ 零依赖，完全适配 C# 5 | 约 150~200 行自维护代码 |
| E. 保持现状（不加框架） | ✅ 零成本 | 上述缺口全部保留 |

- **建议 D 或 B**：D 零依赖、与现有 `run.sh` 无缝衔接；B 更「标准」但要核实 2.6 的兼容性。
- **验证方式**：写一个最小冒烟用例（`Assert.AreEqual(2, 1 + 1)`）能编译、能跑过、失败时退出码非零。
- **预估**：2 小时（含下载与兼容性验证）

### [x] 18. 建立测试工程结构

- 现状：`tests/*.cs` 是裸文件，由 `run.sh` 用 `csc` 逐个编成 exe。
- 目标结构（保持「无 MSBuild」前提）：

```
tests/
├── lib/                        # 第三方 dll（若选 NUnit）
├── framework/                  # 自制的 TestRunner + Assert（若选方案 D）
├── TetrisTests/
│   ├── BlockTests.cs           # 形状表、旋转、随机性
│   ├── GameBoardTests.cs       # 移动、碰撞、合并、消行
│   ├── ScoreTests.cs           # 计分、等级、下落间隔
│   └── UiTests.cs              # 需要消息循环的用例，可单独跳过
└── run.sh                      # 编译 + 运行 + 汇总，改造为支持 --filter
```

- `run.sh` 需要支持按类别/名称筛选（`--filter Board`），否则跑一次 UI 测试要几秒，快速迭代时很烦。
- **预估**：3 小时

### [x] 19. 把现有 44 条断言迁移为测试用例

按被测类重组，不再按「第几轮」组织 —— 轮次是开发顺序，不是测试的合理切面：

| 现有探针 | 迁往 |
| --- | --- |
| `Round1Regression.cs` 的形状随机/配色断言 | `BlockTests` |
| `Round1Regression.cs` 的消行/边界断言 | `GameBoardTests` |
| `Round5Regression.cs` 的计分/等级/速度断言 | `ScoreTests` |
| `Round5Regression.cs` 的高亮状态机 + `Round1Regression.cs` 的按钮/外链 | `UiTests` |
| `Round2Regression.cs` 的 GDI/脏区/残影 | `UiTests`（或单独 `DrawingTests`，依赖 `Panel` 与 `Graphics`） |
| `PixelSmoke.cs` 的像素基线 | `UiTests` |

- **迁移不是复制**：`Round2Regression` 里大量用反射取私有字段（`"now"`、`"bw"`），迁移时应改为使用 `GameController` 的公开属性（`Board` / `CellWidth` / `CellHeight`），减少对内部实现的耦合。
- **预估**：4 小时

### [x] 20. 补齐边界用例（当前完全没有覆盖）

按「分支未覆盖 = 未测试」逐条列举：

**`Block`**

- [x] 形状表完整性：正好 7 种、每种恰好 4 个实心格、全部落在 5×5 内
- [x] `Rotated()` 连续旋转 4 次回到原状
- [x] `Rotated()` 不改变 `Column` / `Row` / `BlockColor`
- [x] `Rotated()` 不消耗随机序列（连做两次旋转后，新建的块形状不受影响）
- [x] 拷贝构造与原块矩阵互不影响（改一个不影响另一个）

**`GameBoard.CanPlace`**

- [ ] 左边界 `column = 0` 且方块最左列有实心格 → 与墙碰撞　**（未覆盖，见第 22 项）**
- [ ] 右边界、底部、顶部对称位置　**（仅测了越界，未测对称贴边，见第 22 项）**
- [x] `column = -1` / `row = -1` / `row` 超出地板 → 返回 false 且**不抛异常**
- [x] 与已落方块碰撞
- [x] 完全空格 → true

**`GameBoard` 移动与合并**

- [ ] `MoveDown` / `Move` / `Rotate` 失败时**状态完全不变**　（`Move` 与 `Rotate` 已覆盖；**`MoveDown` 的状态断言缺失**，见第 22 项）
- [ ] `Merge()` 写入的格子位置正确（含方块矩阵内偏移）　**（只断言了 +4 个格子，未验证位置，见第 22 项）**
- [ ] `Merge()` 不覆盖墙　**（未覆盖，见第 22 项）**

**`GameBoard.ClearLines`**

- [x] 1 / 2 / 3 / 4 行同时满
- [x] 非连续满行（第 10、12 行满，第 11 行不满）都要消掉
- [x] 消行后上方内容**整体下移**且序号正确
- [x] 地板行即使"填满"也不消（`i < lineCount - FloorThickness`）
- [x] 新插入行带墙（`Wall` 在两侧）

**计分与等级**

- [x] 4 行同时消 = 800（不是 4×100）
- [x] 第 9 行 → 第 10 行跨越时的等级与乘数（最易写反的一处）
- [x] 高等级下多行消除的乘数
- [x] `DropInterval`：level 1 / 2 / 12+ 的 clamp 边界　（level 11 的临界点未单独测，见第 22 项）

**构造与状态**

- [x] `GameBoard(0)` / 过小的 `lineCount` → 抛 `ArgumentOutOfRangeException`
- [x] `Advance()` 后 `Next` 一定是新块（不是同一个引用）
- [x] `IsGameOver()` 在判定行边界上的行为

- **预估**：4 小时（用例多但都很短）

### [x] 21. 接入 GitHub Actions（新建仓库后才可行）

- 前四轮时项目只在 gitee，没有 CI 条件；现在已有 `github.com/owlman/Tetris`。
- 工作流要点：
  - runner 用 `windows-latest`（自带 .NET Framework，`csc.exe` 路径与本地一致）；
  - 步骤：检出 → 找 `csc.exe` → 编译 `owlgame` → 编译测试 → 跑 `tests/run.sh`；
  - **UI 测试需要桌面会话**，GitHub runner 是 headless，`Form.Show()` 仍能工作但像素/抓屏类断言会失败 —— 参考第 2 轮的经验，这类用例应标记可跳过。
- **预估**：2 小时（含调试 headless 行为）

---

### [x] 22. 补齐剩余边界用例（第 20 项的遗留）

第 20 项已经写出 57 个用例，但逐条核对清单后发现 6 处仍未真正覆盖。它们的共同点是**看起来测了、实际只测了邻近情况**：

| 未覆盖项 | 现有用例的实际覆盖 |
| --- | --- |
| `column = 0` 且方块最左列有实心格 → 撞左墙 | 只测了 `column = -1`（越界），没测 0 这个**合法列号上的贴墙**情况 |
| 右边界 / 底部 / 顶部的对称贴边 | 只测了 `column = Columns`（越界） |
| `MoveDown` 失败后状态完全不变 | `MoveDownStopsAtTheFloor` 只断言返回 false，**没断言 Row 没变** |
| `Merge()` 写入的格子位置正确 | 只断言「多出 4 个 settled 格子」，**没验证具体落在哪** |
| `Merge()` 不覆盖墙 | 没有用例 |
| `DropInterval` 在 level 11 的临界点 | 只测了 level 1、2 和 level 13（clamp 后），中间的线性段跨过没验证 |

- **为什么留着没做**：这几条都需要按方块实际矩阵算偏移（而不是猜行号），写法比现有用例繁琐，但不做的话「边界」这一类恰恰是**重构时最容易静默坏掉**的地方。
- **预估**：1 小时
- **验证方式**：`bash tests/run.sh --filter Board` 全绿，且新用例在修复前能复现失败。

### [x] 23. 覆盖率审计：逐个 public 成员对照用例

- 现状：57 个用例的分布是凭经验写的，没有对照过 API 清单。`GameController` / `MainForm` 的许多公开成员仍无用例（例如 `CellWidth` / `CellHeight` / `IsHighlighting` 只在 UI 用例里被间接用到）。
- **做法**：把 `Block` / `GameBoard` / `GameController` 的 public 成员列出来，逐条标注「有直接用例 / 只被间接覆盖 / 无用例」，无覆盖的补用例或明确说明不测的理由。
- **注意**：不追求百分比数字 —— 本机没有 .NET SDK，跑不了 dotCover / OpenCover 这类覆盖率工具。这份清单是**人工对照**，目的是找空白点，不是报表。
- **预估**：2 小时

---

### 第 7 轮实测结果（2026-11）

```
单元测试（--skip-ui）: 63 passed, 0 failed, 8 skipped, 71 total
单元测试（完整）      : 71 passed, 0 failed, 0 skipped, 71 total (2028 ms)
像素探针              : gray=17200, ghost 0/80 步差异 —— 均未变
```

**新增 14 个用例**（57 → 71），全部一次通过 —— 说明第 22/23 项补的都是**测试空白**，不是隐藏的缺陷。

#### 第 22 项：6 处边界的补法

关键手法是**不再猜行号/列号**，而是从方块矩阵算出真实填充范围（`MinFilledColumn` / `MaxFilledColumn` / `MinFilledRow` / `MaxFilledRow`），再放两个位置各断言一次：

| 位置 | 期望 |
| --- | --- |
| 最左实心格顶到列 0（墙） | `CanPlace` 返回 false |
| 最左实心格顶到第一个可玩列（`WallThickness`） | 返回 true |

之前只测了 `column = -1` 与 `column = Columns`，那是**越界**检查，跟「合法列号上的贴墙」完全是两回事 —— 方块矩阵的填充列不从 0 开始（I 形从 0 开始，其余从 1 或 2 开始），所以「列号合法」不代表「不撞墙」。

同理补了：右侧贴墙 / 贴地板（最低实心行落在第一行地板上）/ 顶部出界 / `MoveDown` 失败后 Row 与矩阵均不变 / `Merge` 逐格验证绝对位置 / `Merge` 不改墙。

#### 第 23 项：对照表

| 类 | 成员数 | 直接有用例 | 说明 |
| --- | --- | --- | --- |
| `Block` | 6 | 6 | `MatrixSize` 等常量被多处引用 |
| `GameBoard` | 21 | 21 | `ResetWalls` 与 `ClearLines(IList)` 本轮补齐 |
| `GameController` | 15 | 15 | `CellWidth`/`CellHeight`/`Merge`/`FindFullLines`/`HighlightLines`/`ClearLines(IList)`/`Advance`/`IsGameOver` 本轮补齐 |

本轮新增的直接用例：`ResetWalls` / `ClearLines(IList)`（含空列表、越界索引）/ `CellWidth` / `CellHeight` / `ControllerDelegatesTheBoardRules`（一次覆盖 8 个转发方法）。

#### 顺带清掉一个死方法

对照 `GameController` 成员时发现 `public int ClearLines()`（无参）**没有任何调用方** —— 产品代码只用 `ClearLines(IList)` 那个重载（消行动画需要指定行）。这是第 11 项「清理冗余」时漏掉的，已删除。

> 这类成员只有靠**逐个对照调用方**才能发现，测试和编译器都不会报它。

#### 仍不追求的

覆盖率百分比。本机没有 .NET SDK，dotCover / OpenCover 这类工具都用不了。这份对照是**人工清单**，目的是找空白点，不是出报表。

---

### 第 6 轮实测结果（2026-11）

```
单元测试（--skip-ui）: 49 passed, 0 failed, 8 skipped, 57 total
单元测试（完整）      : 57 passed, 0 failed, 0 skipped, 57 total (2268 ms)
像素探针              : gray=17200, ghost 0/80 步差异 —— 均未变
```

**首次运行就有 3 个用例失败**，其中一个是真实缺陷：

| 用例 | 结论 |
| --- | --- |
| `ClearNonAdjacentLines` | **真 bug**：`ClearLines(IList)` 用降序删除，第二次按原始行号删到的是空行，非连续满行只消掉一半。**已修**（改为升序） |
| `CanPlaceRejectsOverlappingSettledCells` | 测试写错：方块填充行不从矩阵第 0 行开始，填第 10 行时方块落在 11~13 行，不重叠 |
| `RotationEitherReplacesTheBlockOrLeavesItAlone` | 测试写错：随机形状无法保证旋转一定失败，改为对成功/失败两种结果分别断言 |

> 这个 bug 从第二轮重构（拆分 `GameBoard`）起就存在，前面所有探针都没抓到，因为它们只断言 `ClearLines()` 的**返回值**等于预期行数 —— 而返回值就是入参长度，删对删错都等于预期值。新用例补了「消完之后不应再有满行」的断言才暴露。
>
> **结论：断言返回值正确，不等于验证了副作用。** 有状态的方法必须断言改变后的状态。

---

## P5 —— UI 设计现代化（2026-11 新增）

**现状盘点**（对着 `MainForm.Designer.cs` 逐项核过，不是凭感觉）：

| 维度 | 当前值 | 问题 |
| --- | --- | --- |
| 窗口 | `ClientSize` 420×392，`FormBorderStyle=FixedDialog` | 固定尺寸，不可缩放；内容多一行就挤 |
| 棋盘 | `Panel` 220×388，`BackColor=(0,0,64)` | 深蓝色块底，与外围系统灰形成硬边 |
| 格子 | 纯色填充 + 1px 描边（`AliceBlue` / `Gray` / 7 色） | 无立体感、无圆角、无间隙，像 8-bit 像素图 |
| 墙 | `Color.Gray`（128,128,128） | 与背景对比度一般，边界靠描边区分 |
| 按钮 | 56×23，`UseVisualStyleBackColor=true` | 尺寸远低于现代可点击目标（建议 ≥ 88×32） |
| 分组容器 | 两个 `GroupBox`（「下一个：」「游戏控制：」） | 老式凹槽边框，视觉噪音大 |
| 分数显示 | `infoLabel` 纯文本堆叠（78×200） | 没有层级，数字与标签同权重 |
| 字体 | 标题 **隶书 24F 粗斜体**、读数 **宋体 9F**、帮助 **楷体_GB2312 12F** | 三套老字体混排；隶书/楷体在现代屏上发糊 |
| DPI | **无感知声明** | 125%/150% 缩放下整窗位图拉伸，文字与格子边缘发虚 |

### [x] 24. 定义目标风格与边界（决策 D7）

**必须先定这四件事，否则后面每一项都会反复**：

1. **改动幅度**：A 只调视觉细节（字体/按钮/间距，布局不动）；B 重排版面（棋盘居中、侧栏卡片化、窗口可缩放）；C 整体重做（含自绘标题栏、暗色主题）。
2. **是否换掉「隶书/楷体」**：换成微软雅黑/思源黑体是最省事的一步，但 2006 年的字体选择本身也是一种年代记录。
3. **棋盘格子是否保留直角像素风**：圆角+间隙更好看，但会让「每格 11×11 像素」的几何关系变复杂（描边、内边距、点击区域）。
4. **是否保留原作者署名**（`myweb` LinkLabel「owlnan制作」）与「说明」独立窗体。

- ⚠️ **与 D4 的冲突**：D4 若选「实验记录（保持 2006 原貌）」，则 P5 整体不应做 —— 现代化会消掉这个项目作为「老代码修复样本」的对照价值。**D7 只能在 D4 选「继续演进」时才有意义。**
- **预估**：0.5 小时（讨论 + 写结论）

### [x] 25. 声明 DPI 感知并支持缩放

- 现状：进程未声明 DPI 感知（探针里要手动 `SetProcessDPIAware()` 才能保证抓屏坐标对齐）。用户在 125%/150% 缩放下运行，整个窗口会被系统位图拉伸 → 文字模糊、格子边缘发虚。
- **做法**：`Program.Main` 里最早调用 `SetProcessDPIAware()`；`MainForm` 的 `AutoScaleMode` 设为 `Dpi`（现在是默认 `Font`，且 `AutoSizeMode=GrowAndShrink`）。
- **风险**：DPI 感知后，`gamewiew` 的 220×388 逻辑像素在高 DPI 下会变小（物理像素不变），格子数 `20 * H / W` 可能变化 → `lineCount` 从 35 变成别的值，**像素基线全废**。需要按 DPI 换算棋盘尺寸，或接受行数变化并更新基线。
- **预估**：2 小时（含在 100%/125% 下各验一遍）

### [x] 26. 控件样式与字体统一

- **按钮**：`FlatStyle=Flat` + `FlatAppearance`（悬停/按下色），尺寸放大到 ≥ 88×32，间距统一；主次按钮用颜色区分（「开始」为主色）。
- **容器**：两个 `GroupBox` 换成自绘卡片（`Panel` + 1px 边框 + 圆角可选），或直接用留白分组替代边框。
- **字体**：统一成一套（建议 `微软雅黑` / `Segoe UI`；中文回退 `Microsoft YaHei UI`），字号分层：标题 14~16pt、读数数字 20~24pt、按钮 9~10pt。
- **注意**：`welcomelabel` 的隶书 24F 粗斜体是「欢迎尝试游戏 / 你失败了…」的覆盖层文字，换字体后 `gaizi` 覆盖层的居中位置需要重新对。
- **预估**：3 小时

### [x] 27. 棋盘绘制视觉升级

改动集中在 `GameController.PaintCell()` 与 `Draw()`：

- **格子间隙**：`cellWidth/cellHeight` 内缩 1px 画填充，留出背景缝 → 视觉上出现网格线，比 1px 描边更干净。
- **圆角**：用 `GraphicsPath` + `AddArc` 画圆角矩形（GDI+ 可用，无需外部库）。圆角半径建议 `min(cellWidth, cellHeight) / 4`。
- **少许立体感**：填充用线性渐变（`LinearGradientBrush`，上亮下暗），或顶部 1px 高光。
- **墙的弱化**：墙从 `Gray(128)` 改成更接近背景的深灰蓝，让「可玩区」更突出；或用内阴影。
- **性能**：`PaintCell` 在每次重绘里对每个格子调用，圆角路径 + 渐变是明显开销。**必须重新跑 `Round2Regression`**（脏区重绘 + 逐像素比对），确认没有残影且帧耗时可接受；必要时缓存每个格子的 `GraphicsPath`。
- **预估**：3 小时

### [x] 28. 消行动画升级

- 现状：`flashTimer` 130ms，把待消行整条刷白（`Color.White`），然后直接消失。
- **可选方案**（按投入排序）：
  1. **亮度呼吸**：高亮色在 130ms 内做一次透明度淡出（`Color.FromArgb(alpha, ...)`）；
  2. **逐行错开**：多行同时消时，每行延迟 30ms 依次闪，视觉上更像「逐行崩塌」；
  3. **收缩/下落**：待消行整行向中间收拢或向下挤压 —— 需要额外的过渡状态与定时器，最复杂。
- **约束**：动画期间 `gametimer` 必须保持暂停（现有状态机已如此），且 `IsHighlighting` 的语义不能破 —— `UiTests.FullLineEntersTheFlashState` 依赖它。
- **预估**：2 小时

### [x] 29. 分数面板（替代 infoLabel 纯文本）

- 现状：`infoLabel` 是一个 78×200 的 `Label`，文本是 `"分数\n0\n\n等级\n1\n\n消行\n0"` —— 标签与数值同字号同颜色，扫视时抓不到重点。
- **做法**：换成自绘面板（或三个独立 `Label` 组合），数值用大号粗体、标签用小号灰色；分数变化时加一次短暂放大/变色反馈。
- **注意**：`UiTests.InfoLabelStartsWithZeroes` 断言 `infoLabel.Text` 包含「分数/等级/消行」—— 改结构后这条用例要同步改（改的是测试，不是产品）。
- **预估**：1.5 小时

### [x] 30. 配色与可访问性

- **色盲友好**：7 种形状色里 `Cyan`（青）与 `Lime`（绿）在红绿色盲下接近；`Magenta` 与 `Blue` 在深蓝背景上对比度偏低。建议改用「形状 + 颜色」双编码（例如给不同形状加内部纹理/描边样式），而不是只靠颜色区分。
- **对比度**：墙 `Gray(128)` 与背景 `(0,0,64)` 的对比度约 4.6:1，勉强够；但 `Blue` 系方块的 `(0,0,255)` 与背景 `(0,0,64)` 对比度很低，容易糊在一起。
- **可验证性**：可以用 WCAG 对比度公式手算（`(L1+0.05)/(L2+0.05)`），不需要工具。
- **预估**：2 小时

### [x] 31. 更新像素测试基线（现代化必然打破现有断言）

**这是 P5 最容易被忽略、也最容易出事的收尾项。** 现有断言直接绑死了像素值：

| 断言 | 当前值 | 现代化后 |
| --- | --- | --- |
| `UiTests.BoardKeepsItsWallsWhilePlaying` | `gray == 17200` | 墙色或格子尺寸一变就失效 |
| `PixelSmoke` 基线 | `gray=17200`、`navy=67760`、`corner=(128,128,128)` | 同上 |
| `Round2Regression` 残影比对 | 逐像素相等 | 圆角/渐变后仍应逐像素相等，但**必须重跑确认** |

- **做法**：把「颜色 → 像素数」的硬编码断言改成**从产品常量推导**（例如墙色取自 `GameController.WallColor`，期望像素数按 `cellWidth × cellHeight` 与墙格数算出），这样改样式时用例自动跟着走，而不是每次手改数字。
- **预估**：2 小时

---

**P5 总预估**：约 16 小时。建议顺序：24（定风格）→ 25（DPI，会动布局尺寸）→ 26（控件/字体）→ 27（棋盘绘制）→ 30（配色）→ 28（动画）→ 29（分数面板）→ 31（更新基线，最后做一次）。

> ⚠️ **动工前必须先定 D7**。若 D7 选「A 只调视觉细节」，25 与 27 可以降级或跳过（DPI 感知会改变 `lineCount`，圆角会改变像素基线，两者都是高风险动作）。

---

### 第 8 轮实测结果（2026-11）

```
单元测试（完整）      : 71 passed, 0 failed, 0 skipped, 71 total (2153 ms)
像素探针              : wall=20812（172 个墙格 x 121 像素）, ghost 0/80 步差异
```

#### 改了什么

| 维度 | 之前 | 现在 |
| --- | --- | --- |
| 窗体 | 420×392，系统灰背景 | **512×412**，浅灰背景 `#F3F3F6` |
| 布局 | 两个 `GroupBox` + 一个纯文本 `Label` | 三张白色卡片：`previewCard` / `controlCard` / `scoreCard` |
| 按钮 | 56×23，系统主题 | **88×34**，`FlatStyle.Flat`，白底描边；「开始/停止」为蓝底白字主按钮 |
| 字体 | 隶书 24F / 宋体 9F / 楷体 | **微软雅黑**（标题 15F、读数 22F 粗体、正文 9F） |
| 分数 | `"分数\n0\n\n等级\n1\n\n消行\n0"` 一个 Label | 三个数值 Label（22F 粗体）+ 三个灰色小标题 |
| 墙 | `Gray(128,128,128)` + 1px 描边 | `(44,44,62)`，**无缝填充**，让可玩区更突出 |
| 方块 | `AliceBlue` 纯色 + 描边 | `(206,214,232)` + 1px 间隙 + 顶部 1px 高光 |
| 消行 | 整行刷白 130ms 后消失 | **暖黄 `(255,236,150)` 分 3 帧淡出**（alpha 235 → 165 → 95 → 25） |
| DPI | 无感知声明 | `SetProcessDPIAware()` + `AutoScaleMode.Dpi` |
| 预览 | 硬编码 `box.Width/4`、`box.Height/9` 定位 | 按 5×5 矩阵**居中**，并把预览框缩到 58×58 |

#### 关键取舍：棋盘几何一律不动

`gamewiew` 仍是 **220×388**。原因很实际 —— `lineCount = 20 × H / W = 35`，格子 `11×11`，这两个数字被像素探针和 `Round2Regression` 的脏区计算同时依赖。改尺寸等于同时动行数、格子数与所有基线，收益却只是「棋盘大一点」。

所以现代化集中在**外围**：卡片、字体、按钮、配色、动画。棋盘内部只换了填色方式（间隙 + 高光）。

#### 像素基线改为「从产品常量推导」

这是第 31 项的核心动作。之前测试这么写：

```csharp
if (p.R == 128 && p.G == 128 && p.B == 128) gray++;
Assert.AreEqual(17200, gray, "wall pixel count is the established baseline");
```

墙色一改，这条就变成 `Expected 17200 But was 0` —— 它报的是「颜色变了」，不是「棋盘没画」。改成从产品推导：

```csharp
Color wallColour = GameController.WallColour;             // 产品暴露的调色板
foreach (cell) if (cell == GameBoard.Wall) wallCells++;    // 从棋盘数墙格
int expected = wallCells * controller.CellWidth * controller.CellHeight;
```

现在换配色只需要改 `GameController`，测试自动跟上；而「棋盘没画出来」仍然会被抓到（墙格数 × 格面积对不上）。

`PixelSmoke` 与 `Round2Regression` 同步做了这个改造，`CountGray` 更名为 `CountWallPixels`。

#### 顺带修掉的两处

1. **预览框的灰色硬边框**：`proimg_Paint` 里留着一圈 `Pen(Color.Gray)` 画的框，现代化后卡片本身已经提供了分隔，这圈灰框成了多余的线。已删。
2. **预览定位的硬编码比例**：`box.Width / 4` + `j * bw` 是近似居中，预览框缩小后偏出边界。改成按 5×5 矩阵算居中偏移，顺便删掉 `PreviewColumnDivisor` / `PreviewRowDivisor` 两个常量。

#### 测试改了 4 条（改的是测试，不是产品）

| 用例 | 原因 |
| --- | --- |
| `InfoLabelStartsWithZeroes` → `ReadoutStartsWithZeroes` | `infoLabel` 拆分成了三个数值 Label |
| `PreviewPaneIsPaintedBeforeTheGameStarts` | 边框没了，改为断言整块是背景色 |
| `BoardKeepsItsWallsWhilePlaying` | 墙像素数改为推导 |
| `FlashTickClearsAndScores` | 淡出动画要 3 次 tick 才清除，改为循环驱动直到 `IsHighlighting` 为假 |

> 最后一条值得注意：动画从「一次性」改成「多帧」后，原先「调一次 `flashTimer_Tick` 就该清完」的假设失效了。测试用 `while (IsHighlighting) { ... }` + 次数上限驱动，既不依赖具体帧数，也不会因为改了帧数而挂。

---

### [x] 32. 帮助窗体风格统一（第 8 轮的遗留）

- 第 8 轮现代化了主窗体，但 `gamehelp` 还留着楷体_GB2312 12F Bold、`GroupBox` 凹槽边框与 75×23 系统按钮。
- 已改为：白卡片 + 微软雅黑 10F/11F + 键帽样式（浅灰方块 + 方向箭头）+ 蓝色扁平「确定」按钮；窗口 266×257 → 300×288。
- 新增用例 `UiTests.HelpWindowMatchesTheModernStyle`：断言字体、四个键帽的箭头文字与扁平按钮，防止以后回退。
- **预估**：1 小时（实际约 40 分钟）

---

### [x] 33. 全面测试与两处状态机缺陷（2026-11）

用独立的探索性探针（不是既有用例）测了四类场景，发现两处真实缺陷：

**缺陷 A：`flashTimer` 没有外部取消路径（4 个症状）**

`flashTimer` 只在自身 `Tick` 里停机，而它的完成路径会调 `EndTurn()` → `gametimer.Enabled = true`。于是动画（135ms）期间的任何用户操作都会出问题：

| 操作 | 症状 |
| --- | --- |
| 动画中点「重玩」 | `flashTimer` 继续跑，待清行的行号指向**旧棋盘**，会去删新棋盘的行 |
| 动画中点「停止」 | 动画跑完后 `EndTurn` 把计时器打开 —— 游戏在「停止」状态下自己恢复 |
| 动画中触发游戏结束 | 计时器没有停下，已结束却仍在运行 |
| 动画中点「说明」 | 打开模态对话框期间后台清行，并把计时器偷偷恢复 |

修复：新增 `CancelPendingClear()`（丢弃动画）与 `CompletePendingClear()`（立即完成清除），待清行从 `Timer.Tag` 移到 `pendingClear` 字段；在 停止 / 重玩 / 游戏结束 / 关闭窗体 / 打开帮助 五条路径上调用。

**缺陷 B：`ClearLines(IList)` 重复行号会重复计分**

传入 `[20, 20]` 时删了一行，却返回 2 并按 2 行给分。公开 API 应去重。已修，并断言「只消 1 行、只得 100 分」。

**验证过、未发现问题的项**：

| 检查 | 结果 |
| --- | --- |
| 200 次重玩 | GDI 句柄 20 → 20，分数/等级/消行与读数均归零 |
| 2000 次随机操作 + 落子消行 | 棋盘行数恒为 35、墙格恒为 172 |
| 帮助窗口创建销毁 50 次 | GDI 20 → 23（WinForms 内部缓存，可接受） |
| `ClearLines(null)` / 越界行号 | 返回 0，不抛异常 |
| `CellAt(-1,0)` / `CanPlace(null)` | 抛异常（对外部误用立即失败，可接受） |
| 动画中四种操作 | 修复后均正确 |

**新增 4 个用例**（72 → 76）：`RestartDuringTheClearAnimationDropsIt`、`StopDuringTheClearAnimationKeepsTheClockStopped`、`GameOverDuringTheClearAnimationStopsTheClock`、`ClearLinesIgnoresDuplicateIndices`。

> 两个修复都做了**回退验证**：把修复临时撤掉，对应用例确实失败（`Expected false, but was true` / `Expected 1 But was 2`），确认测试有效而不是摆设。

- **预估**：2 小时（实际约 1.5 小时）

---

### [x] 34. 对外门面：README 徽章 / 界面截图 / 署名校对（第 9 轮）

GitHub 主页复盘时发现的落差：这个仓有 71 个用例 + CI 全绿 + 像素级回归，但外部看到的只有「0 star、无 description、README 里 0 张图、目录结构连 `tests/` 都没列」。

| 项 | 改动 |
| --- | --- |
| 作者名 | README 第 3 行 `owlnan` → `owlman`（界面 `MainForm.Designer.cs:243`「owlnan制作」→「owlman 制作」） |
| 徽章 | CI（链到 `actions/workflows/ci.yml`）+ License MIT + .NET Framework 4.0 |
| 截图 | 新增 `img/welcome.png` / `playing.png` / `scoring.png`（各 512×412）与「界面」章节 |
| 目录结构 | 补 `tests/`（framework / TetrisTests / run.sh / capture.sh / tools）、`.github/workflows/ci.yml`、`img/` |
| 新脚本 | `tests/capture.sh`（生成截图）、`tests/build-release.sh`（编译并回写 `bin/Debug/owlgame.exe`） |

**截图由真实程序自跑生成**：反射调用 `buttonstart.PerformClick()` 开局，每帧调 `gametimer_Tick` + `DoEvents`，`DrawToBitmap` 截屏，不 mock 任何 UI。为拍到「有堆叠、有分数」的画面，探针内置一个放置评估器（枚举 4 旋转 × 合法列，用产品自己的 `GameBoard.CanPlace` 模拟落点，按聚合高度 / 消行 / 空洞 / 崎岖度打分）。

三代方案对比（前两代均失败）：

| 代 | 策略 | 结果 |
| --- | --- | --- |
| 1 | 每 tick 朝最矮列移动 | 300 局 0 次消行（横跳被自己堆的方块挡住） |
| 2 | 每块只决策一次，移到最矮列 | 80 局 0 次消行（形成差 1 格的阶梯，永不满行） |
| 3 | 放置评估器（模拟落点后打分） | 4~6 局内即可拍到消行 |

**验证**：`bash tests/run.sh` 全通过；`grep -rn owlnan` 0 命中；重编译后 exe 212992 字节；截图日志 `games played: 6, midgame=True, scoring=True`。

**踩坑**：`Block.Column` 是 public 字段不是属性（用 `GetField` 不是 `GetProperty`）；`IsHighlighting` 抓不到高亮帧（`gametimer_Tick` 内部已 `DoEvents`，等检查时高亮已闪完）；Python 改 CRLF 文件时 anchor 用 `\n` 会失配，需先归一化。

- **预估**：1.5 小时（实际约 1 小时，其中 2/3 花在让探针学会自己消行）

---

## [!] 需要用户拍板的决策点

| # | 决策 | 选项 | 影响 |
| --- | --- | --- | --- |
| D1 | 是否做命名现代化（第 7 项） | **已采用 B**：类名与成员名全部 PascalCase，`mainForm` → `MainForm` 同步了 `*.Designer.cs` 与编译时的 `/resource:` 名 | 实测只动了 Designer 里的类声明 / `Name` / `Load` 三处，控件名保持原样 |
| D2 | 是否解耦逻辑与绘制（第 4 项） | **已采用 B**：`GameBoard`（纯逻辑）+ `GameController`（绘制） | 规则现在可直接 `new GameBoard(35)` 断言；2006 年原貌由 git 历史保留 |
| D3 | 闪烁修复是否回到脏矩形局部重绘（第 2 项） | A：保持全量重绘（稳）；B：收窄脏矩形（快，需像素回归） | B 有残影风险；A 性能一般但已验证正确 |
| D4 | 仓库定位 | A：实验记录（保持原貌，只修 bug）；B：可继续演进的玩具项目 | 决定后续投入力度；A 下 P3 功能项大概率不做 |
| D5 | 是否补 LICENSE、用什么协议 | **已采用 MIT**（`Copyright (c) 2006-2026 owlman`） | 允许商用与再分发，与 `CS_StudyNotes` 一致 |
| D6 | 测试框架选型（第 17 项） | **已采用 D（自制框架）**：约 500 行，支持 `[Test]`/`[TestCase]`/`[Category]`/`--filter` | 零依赖、完全适配 C# 5 与 .NET 4.0；xUnit 要求 4.5.2+ 直接排除 |
| D7 | UI 现代化到什么程度（第 24 项） | **已采用 B-lite**：侧栏卡片化 + 字体/按钮/配色全部更新，但 `gamewiew` 固定 220×388 不动 | 保住了 `lineCount=35` 与格子几何；像素基线改用「从产品常量推导」，不再硬编码 |
| D8 | 是否保留 2006 年的年代特征 | **已采用 B**：隶书/楷体/宋体全部换成微软雅黑，灰色描边方块换成带间隙的圆润色块；署名作为唯一的年代标记保留 | 2006 原貌改由 git 历史保留（`4f2e384` 之前的提交）；帮助窗体 `gamehelp` 的楷体**未动**。**第 9 轮修订**：署名字样「owlnan制作」已改为「owlman 制作」—— 那是当年把 `owlman` 拼错的笔误，不是年代特征 |

---

## 建议执行顺序

```markdown
第 1 轮（低风险、纯收益）   1 → 3 → 6 → 16 → 14
第 2 轮（需要回归验证）     2（脏矩形）→ 5（回归脚本）
第 3 轮（结构性改动）       4 → 7 → 8 → 11
第 4 轮（工程卫生）         9 → 10
第 5 轮（视 D4 决策）       12 → 13 → 15
第 6 轮（单元测试）         17（选型）→ 18（结构）→ 20（补用例）→ 19（迁移）→ 21（CI）
第 7 轮（测试收尾）         22（剩余边界）✅ → 23（覆盖率对照）✅   用例 57 → 71
第 8 轮（UI 现代化）       24~31 全部完成（D7-B-lite）✅   用例 71 全过，基线改为常量推导
第 9 轮（对外门面）       34（README 徽章 + 截图 + 署名校对）✅   新增 capture.sh / build-release.sh
```
