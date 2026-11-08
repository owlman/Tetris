# 测试

本项目没有 .NET SDK / MSBuild / NuGet，只有 .NET Framework 自带的 `csc.exe`（**C# 5**）。因此测试分两层：能脱离界面验证的走**单元测试**，需要像素/窗口的走**探针**。

## 一键运行

```bash
bash tests/run.sh                  # 单元测试（含 UI）+ 像素探针
bash tests/run.sh --skip-ui        # 跳过需要桌面会话的 UI 用例（CI 用这个）
bash tests/run.sh --filter Board   # 只跑名字含 Board 的用例
bash tests/run.sh --list           # 只列出用例名
BUILD_DIR=/c/t2 bash tests/run.sh  # 自定义构建目录
```

退出码：全通过为 0，否则非 0。

## 目录

```
tests/
├── framework/                  # 自制测试框架（约 500 行，零依赖）
│   ├── TestAttributes.cs       # [Test] / [TestCase] / [Category]
│   ├── Assert.cs               # AreEqual / IsTrue / Throws / DoesNotThrow ...
│   └── TestRunner.cs           # 反射发现 + 运行 + 汇总
├── TetrisTests/                # 用例
│   ├── BlockTests.cs           # 形状表、随机性、旋转、拷贝构造
│   ├── GameBoardTests.cs       # 墙、落位、移动、合并、消行、回合流转
│   ├── ScoreTests.cs           # 计分表、等级、下落间隔
│   └── UiTests.cs              # 需要窗口的用例（[Category("UI")]）
├── probes/                     # 像素探针（输出基线数字，不是 PASS/FAIL）
│   ├── Round2Regression.cs     # 增量重绘 vs 全绘逐像素比对、GDI 句柄
│   └── PixelSmoke.cs           # 墙体像素守恒、预览框、重玩路径
└── run.sh
```

## 为什么自己写框架（决策 D6）

| 方案 | 结果 |
| --- | --- |
| xUnit | ❌ 要求 .NET 4.5.2+ |
| NUnit 3.x | ⚠️ 要求 .NET 4.5+，与 4.0 运行时可能不兼容 |
| NUnit 2.6.x | ✅ 可用，但要手动下载 dll 并入库 |
| **自制框架** | ✅ 零依赖、完全适配 C# 5、与 `run.sh` 无缝衔接 |

选自制：约 500 行（含注释），支持 `[Test]`、重复 `[TestCase(...)]` 参数化、`[Category("UI")]` 分组、`--filter` 筛选。语法足够用，且不会被框架版本卡住。

运行器输出：

```
[PASS] GameBoardTests.ClearOneLine  0ms
[FAIL] GameBoardTests.ClearNonAdjacentLines  0ms
       Expected 0 item(s), got 1. no line left behind

Results: 56 passed, 1 failed, 0 skipped, 57 total (1889 ms)
```

## 实测基线

```
单元测试（--skip-ui）：64 passed, 0 failed, 8 skipped, 72 total
单元测试（完整）    ：72 passed, 0 failed, 0 skipped, 72 total
```

`PixelSmoke`（220×388 棋盘，`cellWidth=11` / `cellHeight=11`，**35 行**）：

```
after start  : wall=20812 colored=64548
120 key ops  : wall min=20812 max=20812   colored min=64548 max=64548
after restart: wall=20812 colored=64548
preview      : corner=<pane background>
```

**`wall` 一旦下降就代表擦除-重绘间隙出现，即闪烁回来了**。

> `wall = 20812` 是 172 个墙格 × 121 像素（墙无缝填充，11×11 满格）。这个数字本身不写死在测试里 —— 期望值由 `GameController.WallColour`、`CellWidth`、`CellHeight` 与棋盘上的墙格数推导出来，换配色时测试自动跟上。

`Round2Regression`：

```
GDI objects: before=22 after 20 restarts=22
repaint rects: partial=79 full=0
dirty rect covers old+new block: 0 / 80 uncovered
ghost check: steps=80 stepsWithGhost=0 worstDiffPixels=0
```

> `ghost check` 把真实 Paint 事件的脏区重放到内存画布（等价于双缓冲后台缓冲），再与全量重绘逐像素比对。脏区算少 = 残影。
> 不要用抓屏：DWM 半合成帧和 ±1 色阶抖动会产生大量假差异；也不要拿 `DrawToBitmap` 当基准，它走 `WM_PRINT`，笔对齐与屏幕路径差 1 像素。

## 单元测试抓到的真实缺陷

`ClearLines(IList<int>)` 曾用**降序**删除行：

```csharp
for (int k = sorted.Count - 1; k >= 0; --k)   // 错
```

`RemoveAt(line)` 紧跟 `Insert(0, ...)` 的净效果是「索引 < 被删行的上移一格、其余不动」。降序删除时，第一次删除就把较小索引整体上移了，第二次再按原始行号删，实际删到的是空行 —— 非连续满行只能消掉一半。

升序处理才对。这个 bug 在第二轮重构时引入，之前所有探针都没抓到，因为它们只断言 `ClearLines()` 的**返回值**等于预期数量（返回值就是入参长度，无论删对没删对都是对的）。新用例补了「消完之后不应再有满行」的断言，立刻暴露。

## GitHub Actions

`.github/workflows/ci.yml`：`windows-latest` + `--skip-ui`。

CI 只跑逻辑用例 —— runner 是 session 0，没有交互式桌面，`Form.Show()` 与消息循环相关断言不可靠。

## 编写约定

- 测试源码必须是 **UTF-8 with BOM**，否则 `csc` 按 ANSI(936) 读会报一堆 `CS1513 / CS1519` 假语法错误；
- 只用 **C# 5** 语法：不能本地函数、`?.`、`$"..."`；
- 断言文案写清楚「期望什么、为什么」，失败时不用回头读代码；
- 逻辑用例优先直接 `new GameBoard(35)`，不要反射掏 `GameController` 的私有字段 —— 那是 UI 层的事；
- **测边界时不要猜行号/列号**：方块矩阵的填充行列不从 0 开始，先用 `MinFilledColumn` / `MaxFilledColumn` / `MinFilledRow` / `MaxFilledRow` 算出真实范围，再构造「刚好顶到墙」与「刚好贴着最后一个可玩格」两个位置各断言一次。只测 `column = -1` 那种越界情况，证明不了「合法列号上的贴墙」也被拒绝；
- **像素断言要能从产品推导**：别写 `if (p.R == 128 && p.G == 128 && p.B == 128)` 然后硬编码 `17200`。配色一改，测试报的是「颜色变了」而不是「棋盘没画」，等于失去了检测能力。用 `GameController.WallColour` + 墙格数 × 格面积来推导。
