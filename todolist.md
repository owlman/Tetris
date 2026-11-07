# Tetris 项目改进待办清单

> 生成时间：2026-11　基于对全量源码（`owlgame/`，约 700 行）的静态审查 + 两项实测探针验证
> 配套文档：[`AGENTS.md`](AGENTS.md)（构建/编码/验证规范）　[`README.md`](README.md)（项目说明）

**图例**：`[ ]` 待办　`[x]` 已完成　`[!]` 需要用户拍板　`💡` 改进建议（非缺陷）

---

## P0 —— 已实测确认的缺陷，优先修复

### [ ] 1. `Random` 种子重复：开局「当前块」与「下一个块」完全相同

- **现象**：`gameController` 构造函数中相邻创建 `now` 与 `wait` 两个 `block`，两者形状与颜色完全一致（预览框里看到的下一个块就是当前的块）。
- **实测**（探针输出）：

  ```
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

### [ ] 2. 每帧新建 GDI 对象 + 全棋盘重绘（性能）

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

### [ ] 3. 反射调用静默吞掉异常

- **现象**：`EnableDoubleBuffer()` 与 `ApplyFlickerFix()` 均为 `catch (Exception) { }`。一旦反射失败（框架版本差异、控件被裁剪），**闪烁会悄悄回来**，没有任何提示。
- **建议方案**：至少 `catch (Exception ex)` 后 `Debug.WriteLine(ex)` / `Trace.WriteLine`；更稳妥的做法是加一个启动自检开关（环境变量或 `DEBUG` 编译符号），失败时弹窗提示。
- **验证**：临时改一个属性名制造反射失败，确认有可见日志/提示。
- **预估**：0.5 小时

---

## P1 —— 架构与可维护性

### [ ] 4. 逻辑层与绘制层解耦，让核心逻辑可单元测试

- **现状**：`gameController` 直接持有 `Panel view` 并在 `draw()` 里做 GDI 绘制 —— 无法脱离 WinForms 单测，这也是当初只能靠「临时探针 exe」验证的原因。
- **建议方案**（分两步，别一次做太狠）：
  - **第一步**：把棋盘模型抽成 `pure GameBoard`（只管 `map` / `now` / `wait` / 落子 / 消行 / 判定），`gameController` 退化为「模型 + 绘制适配」；
  - **第二步**：为 `GameBoard` 写真正的单元测试（可用 `nunit`/`xunit`，或最朴素的自写断言程序），覆盖：落子边界、消行（单行/连续多行/隔行）、旋转后越界、失败判定。
- **收益**：消行、越界、失败判定这类逻辑可以脱离 GUI 快速迭代（本次评估中「消行逻辑是否只消一行」就是靠 Python 复刻才验证的，本该有 C# 单测）。
- **预估**：半天 ~ 1 天

### [ ] 5. 建立回归验证脚本（替代一次性探针）

- **现状**：验证手段是临时编译的探针 exe（本次放在 `C:\t_probe`），不进仓库 → 下次改代码要重新搭一遍。
- **建议方案**：在 `tests/` 下固化：
  - `tests/board.tests.cs` —— 纯逻辑断言（配合第 4 项）；
  - `tests/pixel.probe.cs` —— 像素级探针（控件文本、预览框边框色、棋盘残影检测）；
  - `tests/run.sh` —— 一键：拷贝源码到纯 ASCII 构建区 → 编译 → 跑断言 → 输出报告。
- **注意**：探针 `.cs` 文件**必须带 UTF-8 BOM**，否则 `csc` 按 ANSI(936) 读取会报一堆无意义的 `CS1513 / CS1519` 语法错误；且 `*.cs` 通配编译前要清掉探针文件，否则 `CS0017` 多入口点。
- **预估**：半天

### [ ] 6. 外部链接未校验 scheme

- **现象**：`mainForm.myweb_LinkClicked` 直接 `Process.Start(url)`，`url` 来自控件 `Tag`（设计器里写死为外链，但无任何校验）。若 `Tag` 被换成本地路径或 `file://`，理论上可拉起本地程序。
- **建议方案**：只允许 `http` / `https`，其余忽略并记日志。
- **预估**：15 分钟

---

## P2 —— 代码风格与工程卫生

### [ ] 7. 命名现代化

- `gameController` / `mainForm` / `block` → `GameController` / `MainForm` / `Block`（C# 规范：大驼峰）
- `rander` → `rander` 拼写错误，应为 `random` / `Rander`
- 常量 `No`（种数）、`size`（矩阵维数）→ `ShapeCount` / `MatrixSize`
- 成员 `map` / `now` / `wait` / `bw` / `bh` → `board` / `current` / `next` / `cellWidth` / `cellHeight`
- **注意**：改名会牵动 `mainForm.cs` / `mainForm.Designer.cs`（`mainForm` 是窗体类名，改名要同步 `.Designer.cs` 与 `.resx` 里的 `mainForm` 资源前缀）
- **预估**：1 小时（可与第 4 项一并做）

### [ ] 8. 消除 magic number

- 棋盘控件尺寸 `220×388`、预览框 `79×53`、预览定位 `box.Width/4` / `box.Height/9`、列数 `20`、颜色 `AliceBlue` / `Gray` 全部硬编码。
- **建议方案**：抽为 `const` / `static readonly`，或从布局数据推导（如预览框位置按棋盘中心对齐，而非硬编码系数）。
- **预估**：1 小时

### [ ] 9. 清理仓库中误跟踪的构建产物与噪声

- 当前被跟踪：`owlgame/bin/Debug/owlgame.vshost.exe`、`owlgame/bin/Debug/elsfk(by .NET).rar`、`owlgame/obj/**` 编译产物。
- **建议方案**：`git rm --cached` 移出版本控制 + 补 `.gitignore`（注意 `.gitattributes` 里是 `* -text`，行尾不会被规范化，移除产物不影响行尾一致性）。
- **注意**：`owlgame/bin/Debug/owlgame.exe` 与 `owlgame.pdb` **是有意跟踪**的（老项目惯例，保证 clone 即可运行），不要误删。
- **预估**：30 分钟

### [ ] 10. 补 LICENSE

- 仓库无许可证文件。若要公开到 gitee/github，建议补一个 MIT（与 `CS_StudyNotes` 一致）或明确标注「仅供学习交流」。
- **预估**：10 分钟

### [ ] 11. 清理冗余代码

- `clearLine()` 消行后调用 `initMap()` 全图重扫 —— 功能正确但纯属冗余（墙的初始化本就不该在消行后重做）。
- `draw()` / `drawwait()` 中 `Pen` / `SolidBrush` 的手工 `Dispose`（若做了第 2 项改为 `using` 或字段持有）。
- **预估**：30 分钟

---

## P3 —— 功能补全（可选，视目标而定）

### [ ] 12. 计分与等级

- 无任何计分。建议：按消行数计分（1/2/3/4 行分别给不同分值）、连消加倍，每消除 N 行升一级并加快下落速度。
- **预估**：2 小时

### [ ] 13. 消行动画与音效

- 当前消行是「瞬间消失 + 整体下移」，无过渡。最小实现：在 `clearLine()` 前后插一个短定时器状态，先把待消行刷成高亮色停留 100~200ms，再真正移除。
- **预估**：半天

### [ ] 14. 方块颜色按形状固定

- 现在每个方块**随机取色**（红/黄/蓝三色之一），不符合俄罗斯方块惯例（同形状同色）。
- **建议方案**：把 7 种形状与固定颜色一一对应（`blockbox` 已按索引组织，改造成本低）。
- **预估**：30 分钟

### [ ] 15. 下落速度与手感

- `gametimer.Interval = 1000`（1 秒/格），偏慢；按键 `↓` 一次下移 2 格。
- **建议方案**：随等级动态调整间隔，或改为「按时间累计下落距离」的模型（帧率无关）。
- **预估**：1 小时

### [ ] 16. Game Over 后无法直接重开

- 失败分支里 `buttonstart.Enabled = false`，用户只能点「重玩」。
- **建议方案**：失败时把「开始」按钮恢复可用并改文案，或让「重玩」成为默认焦点。
- **预估**：10 分钟

---

## [!] 需要用户拍板的决策点

| # | 决策 | 选项 | 影响 |
| --- | --- | --- | --- |
| D1 | 是否做命名现代化（第 7 项） | A：只改局部成员名；B：连类名一起改（含 Designer/resx 联动） | B 更规范但改动面大，Designer 文件是设计器生成的，手改后与设计器状态不一致 |
| D2 | 是否解耦逻辑与绘制（第 4 项） | A：保持现有单文件结构；B：拆出 `GameBoard` | B 带来可测试性，但偏离「保持 2006 年原貌」的实验记录价值 |
| D3 | 闪烁修复是否回到脏矩形局部重绘（第 2 项） | A：保持全量重绘（稳）；B：收窄脏矩形（快，需像素回归） | B 有残影风险；A 性能一般但已验证正确 |
| D4 | 仓库定位 | A：实验记录（保持原貌，只修 bug）；B：可继续演进的玩具项目 | 决定后续投入力度；A 下 P3 功能项大概率不做 |
| D5 | 是否补 LICENSE、用什么协议 | MIT / 仅供学习交流 / 不补 | 影响能否公开分发 |

---

## 建议执行顺序

```
第 1 轮（低风险、纯收益）   1 → 3 → 6 → 16 → 14
第 2 轮（需要回归验证）     2（脏矩形）→ 5（回归脚本）
第 3 轮（结构性改动）       4 → 7 → 8 → 11
第 4 轮（工程卫生）         9 → 10
第 5 轮（视 D4 决策）       12 → 13 → 15
```

**第 1 轮全部改动都在 1 小时内可完成**，且不改变任何现有架构；做完再用现有的像素探针跑一遍回归即可。
