# AGENTS.md

本文件是《Tetris / 俄罗斯方块》（C# + WinForms）实验项目的 Agent 工作指引。

项目路径：`D:\Working\writing\0_LabNotes\游戏开发\01.实验项目\C#/Tetris\`

---

## 项目是什么

一个 2006 年浙大（zju）作者用 C# + WinForms 写的俄罗斯方块小游戏，**画面全部由 GDI 手工绘制**，而不是用控件摆放方块。原始版本存在"按键操作时画面闪烁"的缺陷，2026-11 已定位并修复（详见"闪烁修复"一节与配套实验笔记）。

- 玩法：方向键 ↑ 旋转、← → 移动、↓ 加速下落；「开始」启动、「重玩」重置、「说明」看帮助。
- 计分：1/2/3/4 行分别 100/300/500/800 分 × 当前等级；每 10 行升一级，下落间隔 1000 ms 起、每级 -90 ms、下限 120 ms。
- 消行有一段**淡出动画**（`flashTimer`，45 ms × 3 帧，alpha 235→165→95→25），动画期间 `gametimer` 暂停。
- 界面为现代浅色卡片风格（2026-11 第 8 轮）：微软雅黑、扁平按钮、三张白色卡片（预览 / 控制 / 读数）；棋盘为深色无缝墙 + 带间隙方块。
- 帮助窗体 `gamehelp` 同一套风格（白卡片 + 键帽样式 + 扁平按钮），由 `UiTests.HelpWindowMatchesTheModernStyle` 锁定；**新增任何窗体都请沿用这套配色与字体**。
- 棋盘 20 列 × **35 行**，7 种方块，每种形状固定一种颜色。
  - 行数 = `rowNo * v.Height / v.Width` = `20 * 388 / 220` = **35**（整数除法）。旧文档写的 38 是错的。
  - 格子 11×11 像素（`bw = 220/20`，`bh = 388/35`）。
- 同一 `游戏开发` 目录下还有一个姊妹实现：`../python/tetris.py`（Python + pygame 版），两者互不引用。

配套实验笔记（**改动本项目时应同步考虑是否需要更新**）：
`../../02.实验笔记/C# 版俄罗斯方块 bug 修复之WinForms 自绘控件闪烁问题的定位与修复.md`

---

## 仓库拓扑（重要，先搞清楚再动手）

本项目是**嵌套在另一个 git 仓库里的独立 git 仓库**：

| 层 | 路径 | 仓库 | remote | 分支 |
| --- | --- | --- | --- | --- |
| 内层（本项目） | `.../C#/Tetris/` | 独立 git 仓 | `origin` → gitee `owlman/owlgame-Tetris`（HTTPS）<br>`github` → `owlman/Tetris`（SSH） | `master` |
| 外层（笔记库） | `0_LabNotes/` | 独立 git 仓 | **无 remote** | `master` |
| 更外层 | `D:\Working\writing\` | **不是 git 仓** | — | — |

> 本项目有**两个远端**，每次同步必须全推：
> ```bash
> git push origin master && git push github master
> ```
> `github` 用 SSH（`git@github.com:owlman/Tetris.git`），`origin` 用 HTTPS（gitee）。
> GitHub 仓库为 Public，许可证 MIT。

因此：

- 在 `Tetris/` 里跑 `git` 操作只影响本项目；在 `0_LabNotes/` 里跑只影响笔记库。**不要跨层假设**。

### 🚫 硬规则：不管外层 `0_LabNotes` 的 git

**用户 2026-11 明确要求：只管本 C# 项目，外层实验笔记库的 git 管理一律不碰。**

- ✅ 只在 `.../C#/Tetris/` 内执行 `git status / add / commit / push`（remote：`https://gitee.com/owlman/owlgame-Tetris`）。
- ❌ **不要**在 `0_LabNotes/`（或更外层）执行 `git add` / `git commit` / `git push` / `git rm --cached` / 改 `.gitignore`。
- ❌ 不要因为「笔记与项目内容对不上」就主动去提交外层仓 —— 需要时**告知用户**，由用户决定。
- 可以**读**外层笔记（如 `../../02.实验笔记/*.md`）作为上下文，也可以在笔记里写内容；但**不代管它的版本控制**。
- 若发现外层仓有未提交改动（`git status` 不干净），**不要顺手清理或提交**，直接忽略即可。

> 注：外层 `0_LabNotes` 仓**没有配置 remote**，本项目的改动不会自动进入笔记库版本历史。

---

## 目录结构

```
Tetris/
├── owlgame/                 # 唯一的项目目录（项目名 MuscoviteBlock，程序集名 owlgame）
│   ├── MuscoviteBlock.csproj # VS2005 时代的老式 csproj（Format 12.00，无 SDK-style）
│   ├── Program.cs           # 入口：EnableVisualStyles + Run(new MainForm())
│   ├── MainForm.cs          # 主窗体逻辑（自绘派发的唯一入口）
│   ├── MainForm.Designer.cs # 主窗体布局（设计器生成，勿手改）
│   ├── GameBoard.cs         # **纯逻辑**：棋盘数据 + 移动/旋转/落子/消行/失败判定（不引用 WinForms）
│   ├── GameController.cs    # **绘制**：像素几何 + GDI 资源 + 脏矩形重绘，规则全部委托给 GameBoard
│   ├── Block.cs             # 方块定义：7 种形状的 5×5 矩阵 + 形状固定配色
│   ├── gamehelp.cs(+Designer) # 「说明」帮助窗体
│   ├── MainForm.resx / gamehelp.resx
│   ├── Properties/          # AssemblyInfo / Resources / Settings
│   ├── bin/Debug/           # 仅 owlgame.exe / owlgame.pdb 被跟踪（clone 即可运行）；vshost 已移出
│   └── obj/                 # 构建中间产物，**已全部移出版本控制**并由 .gitignore 忽略
├── Backup/                  # 未跟踪：修复前的原始代码快照
├── Backup1/                 # 未跟踪：闪烁修复后的中间快照
├── temp/                    # 未跟踪：临时产物（backup_bin/ 存原版 exe）
├── temp_build/              # 未跟踪：空的构建暂存目录
├── .vs/                     # 未跟踪：VS 2026 本地状态
├── LICENSE                  # MIT License（2006-2026 owlman）
├── README.md                # 项目简介
├── owlgame.sln / owlgame.suo
└── UpgradeLog.htm(2)       # VS 升级迁移报告，噪声文件
```

**易踩的坑**：`owlgame/bin/Debug/owlgame.exe` 是**被版本控制跟踪的**。用户有时直接双击这个旧二进制运行，导致"改了代码却没效果"。**任何源码改动后必须重新编译并覆盖它**（见"构建"一节）。

---

## 构建（没有 msbuild，必须用 csc）

本机 **没有安装 .NET Framework SDK**，`MSBuild.exe` 走 `Microsoft.Common.targets` 会因找不到 `resgen.exe` 报错 MSB3091：

```
C:\WINDOWS\Microsoft.NET\Framework64\v2.0.50727\Microsoft.Common.targets(1626,9): error MSB3091:
resgen.exe 未能找到 ... 未安装 .NET Framework SDK
```

可用工具只有 .NET Framework 4.0 自带的编译器：

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

### 标准构建流程（纯 ASCII 路径）

**`csc.exe` 不支持含中文的路径**（本项目路径里有 `游戏开发`，会报 `CS2001: 未找到源文件`）。必须先复制到纯 ASCII 目录再编译：

```bash
# 1) 复制源码到纯 ASCII 路径（工作区在 C:\t_build）
cd /c/t_build
cp "/d/Working/writing/0_LabNotes/游戏开发/01.实验项目/C#/Tetris/owlgame/"*.cs .
cp "/d/Working/writing/0_LabNotes/游戏开发/01.实验项目/C#/Tetris/owlgame/Properties/"*.cs Properties/
cp "/d/Working/writing/0_LabNotes/游戏开发/01.实验项目/C#/Tetris/owlgame/"*.resx .

# 2) 编译（注意 /main、三个 /resource、图标）
MSYS_NO_PATHCONV=1 /c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe \
  /nologo /target:winexe /main:owlgame.Program \
  /out:owlgame.exe /debug /define:TRACE,DEBUG \
  /win32icon:"Core IP theme Icon 01.ico" \
  /resource:MainForm.resx,owlgame.MainForm.resources \
  /resource:gamehelp.resx,owlgame.gamehelp.resources \
  /resource:Properties/Resources.resx,owlgame.Properties.Resources.resources \
  *.cs

# 3) 回写被跟踪的产物
P="/d/Working/writing/0_LabNotes/游戏开发/01.实验项目/C#/Tetris"
cp owlgame.exe "$P/owlgame/bin/Debug/owlgame.exe"
cp owlgame.pdb "$P/owlgame/bin/Debug/owlgame.pdb"
```

要点：

- `MSYS_NO_PATHCONV=1` 避免 Git Bash 把 `/out:` `/resource:` 这类参数当成路径转换掉。
- `/main:owlgame.Program` 必须给：目录里若残留测试用的 `harness*.cs` / `check*.cs`（各自带 `Main`），不指定会报 `CS0017: 程序集有多个入口点`。**编译前删掉测试脚本**。
- 三个 `.resx` 必须以程序集限定名嵌入，否则运行时报 `MissingManifestResourceException`。
- 图标是 `Core IP theme Icon 01.ico`（161 KB，VS 升级产物），用 `/win32icon:` 才能带上，否则 exe 图标丢失。
- `/define:TRACE,DEBUG` 不能省：`System.Diagnostics.Trace.WriteLine` 带 `[Conditional("TRACE")]`，**不定义常量时整个调用会被编译器移除**，诊断输出等于空操作。

---

## 源码编码矩阵（2026-11 已统一）

**历史背景**：项目源于 .NET Framework 2.0 / VS2005 时代，`mainForm.cs` 与 `block.cs` 原本是 **GBK 无 BOM**，其余是 UTF-8，混用导致中文在不同编辑器与 `csc` 之间反复变成乱码。

**2026-11 已把全库 `.cs` 统一为 UTF-8 with BOM + CRLF**，`mainForm.cs` / `block.cs` 由 GBK 转码而来（内容语义未变，已用逐行 diff 校验）。

现行约定：

| 文件 | 编码 | BOM | 行尾 |
| --- | --- | --- | --- |
| `owlgame/*.cs`（全部，含 `Properties/`） | UTF-8 | **有** | CRLF |
| `owlgame/*.resx` | UTF-8（内容纯 ASCII） | 无 | CRLF |
| `*.md`（README / AGENTS / todolist / tests） | UTF-8 | 无 | **CRLF** |
| `.gitattributes` / `.gitignore` | UTF-8 | 无 | LF（git 自身的习惯） |

`.gitattributes` 里写了 `* -text`，禁用 git 的行尾自动转换 —— 仓库中保存的字节与工作区**完全一致**，避免在 Windows / Linux 之间同步时出现整文件行尾 diff。

> ⚠️ **带 BOM 是刻意的**：`csc.exe` 与旧版 MSBuild 读无 BOM 的源文件时按系统 ANSI 代码页（936）解释，UTF-8 中文会直接编译成乱码；带 BOM 后编译器能无歧义地按 UTF-8 读取。

规则：

1. 改**任何** `.cs` 一律按 **UTF-8 with BOM + CRLF** 读写。
2. Python 脚本读写时用 `encoding='utf-8-sig'`（读）/ `encoding='utf-8-sig'`（写）+ `newline='\r\n'`，并用默认 `newline=None` 归一化行尾（用 `newline=''` 保留 `\r\n` 会导致用 `\n` 构造的匹配串**永远匹配不上**）。
3. **新增注释一律写英文**，可以顺带规避整个编码问题。
4. 改写前确认文件在版本控制里（`git status` + 文件大小自检），出事可 `git checkout HEAD -- <path>` 恢复。
5. **`.md` 文档也要按 CRLF 写**（本仓 `.md` 已统一 CRLF）。`write` 工具写出的文件是 **LF**，用它整篇重写 `.md` 会把全文件行尾从 CRLF 翻成 LF，制造几百行的纯行尾 diff —— 本仓历史上 `948581c` 就这么产生过 580 行噪音。**改 `.md` 请用 Python 脚本按 `newline='\r\n'` 写，不要用 `write` 整篇覆盖。**

```python
# .cs 文件的正确改法（UTF-8 BOM + CRLF）
with open(path, 'r', encoding='utf-8-sig', newline=None) as f:
    text = f.read()          # 默认 newline=None，\r\n 归一化为 \n
text = text.replace(old, new, 1)
with open(path, 'w', encoding='utf-8-sig', newline='\r\n') as f:
    f.write(text)
```

> 临时探针 / 脚本若是**新建**的 `.cs`，同样要加 UTF-8 BOM，否则 `csc` 会按 ANSI 读并报出一堆无意义的 `CS1513 / CS1519` 语法错误（症状与真实语法错误完全不同，极易误判）。
> 另注意 `csc` v4.0.30319 是 **C# 5** 编译器：本地函数（local function）、`?.`、`$"..."` 插值等 C# 6+ 语法一律不支持。

---

## 架构要点

### 四个类，逻辑与绘制分离（第 3 轮重构）

```
MainForm (UI + 事件)
   │  持 controller 引用；controller == null 表示"未开局"
   ├─ gamewiew_Paint(e)  ──► controller.Draw(e)              画主棋盘（Panel 自绘）
   ├─ proimg_Paint(e)    ──► controller.DrawPreview(proimg,e) 画"下一个"预览（PictureBox 自绘）
   └─ gamewiew_PreviewKeyDown ─► controller.Rotate()/Move()/Down()

GameController (绘制层，引用 WinForms)
   ├─ board : GameBoard     规则全部委托给它
   ├─ cellWidth / cellHeight : int   由 view 宽高算出
   ├─ Draw() / DrawPreview()        两个 GDI 绘制入口
   └─ InvalidateBlock()     只失效「旧位置 ∪ 新位置」

GameBoard (纯逻辑层，**不引用 System.Windows.Forms**)
   ├─ cells : List<int[]>   棋盘数据，Empty=0 / Settled=1 / Wall=2
   ├─ Current / Next : Block  当前块 / 下一个块
   ├─ Move / MoveDown / Rotate / Merge / ClearLines / Advance / IsGameOver
   ├─ FindFullLines()       列出满行（供消行动画先用，不修改棋盘）
   ├─ Score / Level / Lines / DropInterval   计分与难度
   └─ CanPlace()            带显式边界检查（旧代码会越界崩溃）

Block (数据 + 形状表)
   └─ Matrix[5,5] / Column / Row / BlockColor
```

**为什么这样拆**：`GameBoard` 不依赖 WinForms，规则可以直接 new 出来断言（`Round1Regression` 现在就是这么测消行与边界），不必再靠反射掏 `controller` 的私有字段。

### 关键状态约定

- **`controller == null` 是"未开局"哨兵**。`MainForm` 里所有 Paint 分支都靠它判断要不要画。改了 Paint 逻辑要连带检查这个分支（见下面 proimg 的教训）。
- `gamewiew` 是 `Panel`，**默认不开双缓冲**；`proimg` 是 `PictureBox`，**默认开双缓冲**。这是实测结论。
- 类名已统一为 PascalCase（`MainForm` / `GameController` / `GameBoard` / `Block`），**控件名仍是设计器给出的小写**（`gamewiew` / `proimg` / `gaizi` / `gametimer`）—— 控件名改了要同步 `*.Designer.cs`，不要顺手改。
- **两个定时器的分工**：`gametimer` 负责下落（间隔随等级变化），`flashTimer` 是消行动画的一次性定时器。消行流程是 `gametimer_Tick` 发现满行 → 关掉 `gametimer`、`HighlightLines()` + 启动 `flashTimer` → `flashTimer_Tick` 里真正 `ClearLines()` 并调 `EndTurn()`。**不要跳过 flash 直接 `ClearLines()`**，否则动画失效；也不要在动画期间让 `gametimer` 继续跑，新方块会盖在动画上。
- 读数由三个独立 Label 承担：`scoreValue` / `levelValue` / `linesValue`（另有 `scoreCaption` 等三个灰色小标题）。`UpdateStatus()` 是唯一刷新它们的地方，改计分逻辑后记得同步。
- **配色一律定义在 `GameController` 且对外暴露**（`WallColour` / `SettledColour` / `HighlightColour` / `CellGapPixels`）。像素测试从这些常量推导期望值 —— 换配色只改这里，不要去改测试里的数字。
- **`gamewiew` 固定 220×388 不要动**：`lineCount = 20 × H / W = 35`、格子 `11×11`，这两个数被脏区计算与像素基线同时依赖。现代化改外围，不改棋盘几何。
- 进程已声明 DPI 感知（`Program.Main` 里 `SetProcessDPIAware()`），`AutoScaleMode = Dpi`。
- `gaizi` 是覆盖在 `gamewiew` 上的 `Dock=Fill` Panel，开局时 `Hide()`、结束时 `Show()` 显示"欢迎尝试游戏"/"游戏失败啦"。
- `gamewiew.AutoSize` 已从 `true` 改为 `false`（原本与内部 `Dock=Fill` 子控件形成递归布局约束）。

### 闪烁修复（已落地，不要回退）

原问题：按键时棋盘闪烁。定位到三个根因——擦除-重绘间隙、脏矩形偏移写错（`(j + now.curx - 4) * bw` 多减了 4 格）、`AutoSize` 与 `Dock=Fill` 递归约束。

已实施的修复：

1. `EnableDoubleBuffer()` —— 反射设置 `Control.DoubleBuffered`，**递归到所有子控件**。
2. `ApplyFlickerFix()` —— 反射 `SetStyle` 追加 `OptimizedDoubleBuffer | AllPaintingInWmPaint | Opaque`，断掉 `WM_ERASEBKGND`。
3. **`Opaque` 是一个契约**：声明"我覆盖每一个像素"，因此**自绘函数必须自己铺满整个背景**。`draw()` / `drawwait()` 开头都加了 `FillRectangle(bgBrush, ClientRectangle)`。
4. ~~`Invalidateblock()` 改为整块 `view.Invalidate()`~~ —— **第 2 轮已改回脏矩形局部重绘**（见下）。
5. `gamewiew.AutoSize = false`。

### 局部重绘与 GDI 缓存（第 2 轮，不要退回全屏重绘）

- `InvalidateBlock(Rectangle before)`：调用方传入**移动前**块的外接框，方法内部与移动后的外接框取 `Rectangle.Union`，只失效并集区域。偏移公式必须是 `(j + now.curx) * bw` —— 历史 bug 写成 `(j + now.curx - 4) * bw`，多减了 4 整格。
- `draw()` 只遍历与 `e.ClipRectangle` 相交的格子（由脏区反推 `i0..i1 / j0..j1`），但**背景填充仍必须覆盖整个脏区**（`Opaque` 契约）。
- `Pen` / `SolidBrush` 提为字段，构造时创建、`Dispose()` 释放；`mainForm.ResetController()` 在开新局时释放旧的 controller，否则句柄会泄漏。
- ⚠️ 改这里必须跑 `tests/Round2Regression.cs`：它把真实 Paint 事件的脏区重放到内存画布，再与全量重绘逐像素比对。**脏区算少 = 残影，比闪烁更难察觉。**

> ⚠️ **改动自绘函数时必须记住 Opaque 契约**：任何一个 Paint 分支"什么都不画"，屏幕就会残留脏像素。
> 实例：`proimg_Paint` 原本是 `if (controller != null) controller.drawwait(...)`，初始界面 `controller == null` → 一帧不画 → 因为 `proimg` 带 `Opaque` 标志、Windows 不再替它擦背景 → **「下一个」下面那块预览窗格整个消失**。修法是先无条件铺 `proimg.BackColor`，再画预览，最后补一圈灰色边框。改动此函数时务必覆盖"未开局"与"游戏中"两个状态。

---

## 验证方法（本项目没有单元测试，靠探针）

> 📁 **测试已工程化**，一键运行：`bash tests/run.sh`（`--skip-ui` / `--filter` / `--list`）
> - `tests/framework/` —— 自制测试框架（`[Test]` / `[TestCase]` / `[Category]` / `Assert` / 反射运行器），零依赖、C# 5 兼容
> - `tests/TetrisTests/` —— 57 个用例：`BlockTests` / `GameBoardTests` / `ScoreTests` / `UiTests`
> - `tests/probes/` —— 像素探针，输出基线数字（不是 PASS/FAIL）：`Round2Regression`（残影逐像素比对、GDI 句柄）、`PixelSmoke`（墙体守恒）
>
> 逻辑用例直接 `new GameBoard(35)`，不要反射掏 `GameController` 的私有字段。UI 用例标 `[Category("UI")]`，headless 环境用 `--skip-ui` 跳过。脚本、基线、编写约定见 `tests/README.md`。

不能靠肉眼看、也不能靠抓帧（闪烁是瞬态的，`DoEvents()` 之后画面已稳定，抓帧必然抓到正常帧）。可用的两条路：

### A. 像素级探针（推荐，最快）

写一个临时 exe，实例化 `mainForm` → `Show()` → `DoEvents()` → 用 `DrawToBitmap` 抓控件像素并断言颜色：

```csharp
FieldInfo fi = typeof(owlgame.mainForm).GetField("proimg",
    BindingFlags.Instance | BindingFlags.NonPublic);
PictureBox box = (PictureBox)fi.GetValue(f);
Bitmap bmp = new Bitmap(box.Width, box.Height);
box.DrawToBitmap(bmp, new Rectangle(0, 0, box.Width, box.Height));
// 断言：窗格内部应为深蓝 0xFF000040，边框应为灰 0xFF808080
```

验证"下一个"预览窗格时的实测基线：

```
y=0 : FF808080 FF808080 ...   ← 灰色边框
y=12: FF808080 FF000040 ...   ← 深蓝窗格
y=24: FF808080 FF000040 FFFFFF00 ...  ← 开局后中央出现黄色预览方块
```

`mainForm` 的私有字段（`proimg`、`gamewiew`、`controller`）用反射访问。

### B. Win32 消息计数（定位闪烁类问题时）

子类化窗口过程统计 `WM_ERASEBKGND` / `WM_PAINT` 次数与 `GetUpdateRect` 覆盖率。
修复后的基线：`WM_ERASEBKGND` 从 90 降到 1（剩余 1 次发生在构造函数执行前，样式标志尚未生效，属预期）。

### C. 端到端冒烟

以"墙体像素数"作为画面完整性探针（墙是静止深灰色，正常时恒为 17200；一旦消失说明画面被擦掉了）：

```
after start  : gray(walls)=17200 colored(blocks)=400  navy=67760
after play   : gray(walls)=17200 colored(pile)=800    navy=67360
after restart: gray=17200 colored=400 navy=67760
```

---

## 临时文件与工作区纪律

- 临时脚本 / 探针 exe / 截图放 `temp/`（本项目内，未跟踪）或 `C:\t_build`（纯 ASCII 构建区）。
- **不要**把临时产物放进 `owlgame/` 目录——`owlgame/bin`、`owlgame/obj` 是被跟踪的，污染后 git diff 会很难看。
- `Backup/`、`Backup1/` 是手工快照目录，**不要提交**（当前未跟踪，保持未跟踪）。
- 编译测试脚本（`harness*.cs`、`check*.cs`）**不要留在 `C:\t_build`**，会导致下次 `csc *.cs` 报多入口点。

---

## 每轮改动后的笔记与 todolist 同步（硬规则）

**每完成一轮修改（一轮 = `todolist.md` 里的一个批次，或用户指定的一个任务），必须同时更新「实验笔记」和 `todolist.md`，两件都要做，不要等用户提醒。**

| 要更新的东西 | 位置 |
| --- | --- |
| 实验笔记 | `../../02.实验笔记/C#版俄罗斯方块：agent 修改记录.md` |
| 待办清单 | 本项目根目录 `todolist.md` |

### 实验笔记：每轮记录至少包含四部分

| 部分 | 写什么 |
| --- | --- |
| 改了什么 | 逐项对应 todolist 编号；贴出**改动前**的关键代码与**改动后**的写法 |
| 为什么错 | 根因（不是「现象」的复述），例如 `Random()` 用 `TickCount` 做种子、`[Conditional("TRACE")]` 会让调用消失 |
| 怎么验的 | 探针断言输出 / 像素基线数字 / 消息计数，贴实测值而不是「已验证」 |
| 踩了什么坑 | 这一轮里真正浪费时间的东西，包括自己写错的探针断言 |

### `todolist.md`：四个动作

1. 已完成的条目把 `[ ]` 勾成 `[x]`（只勾真正做完并验证过的，做了一半的不勾）；
2. 顶部「进度」行写明已完成到第几轮、哪些编号；
3. 部分完成或情况有变的条目，更新它的「现状」描述（例如「探针已固化，缺一键脚本」→ 补脚本后应改为完成）；
4. 若这轮发现了新问题，追加为新条目，别只写在笔记里。

### 其它要求

- 笔记末尾的「下一轮建议」—— 换成下一批次实际要做的事（含风险提示）。
- 顺序：先改代码 → 跑 `bash tests/run.sh` → 再写笔记与 todolist（避免记录了未验证的结论）。

> 🚫 注意：**只写笔记与 todolist 的内容，不提交外层 `0_LabNotes` 仓**（见「仓库拓扑」的硬规则）。`todolist.md` 在本项目仓内，随本轮 commit 一起提交。

---

## Git 约定

- 内层仓库 `master` 分支，**两个远端都要推**：`origin` → gitee `owlman/owlgame-Tetris`，`github` → `owlman/Tetris`（Public，MIT）。
  ```bash
  git push origin master && git push github master
  ```
  只推一个会让两个公开仓的代码分叉，给 `clone` 下来的人不同的版本。
- 提交前跑 `git status -sb`，注意 `owlgame/bin/Debug/owlgame.exe`、`owlgame.pdb` 是被跟踪的二进制，**源码改动要一并重新编译并提交**（否则仓库里的 exe 与源码不一致，下次别人运行还是旧行为）。
- commit message 遵循 Conventional Commits；仓库历史是英文短句风格（`first commit` / `add README` / `this is a game` / `vs 2008 file...`），新提交建议用 `fix:` / `chore:` / `docs:` 等标准前缀。
- **不要**主动推送外层 `0_LabNotes` 仓库（它没有 remote，且 `游戏开发/` 目录当前是未跟踪状态，是否纳入由用户决定）。
- 文件重命名一律 `git mv -f`（Windows 大小写不敏感，`rm -f` + `git add` 会删掉唯一副本）。

---

## 已知的"看起来像 bug 但其实是设计"的地方

| 现象 | 真相 |
| --- | --- |
| `gamecontroller` 里 `rowNo = 20` 但棋盘是 20×35 | 列数固定 20，**行数由 `v.Height / v.Width * rowNo` 算出**（220×388 的控件 → `20*388/220` = 35 行） |
| `drawwait` 里用 `box.Width/4 + j*bw` 定位预览 | 预览框比棋盘小得多，位置是硬编码的近似居中，不是数学居中 |
| `block.size = 5` 但只有 7 种形状 | 5×5 矩阵里实际只占 2×4 / 3×2 等一小块，其余是 0 |
| `gamecontroller.cs` 里有 `using System.Windows.Forms` | 为了用 `Panel` / `PictureBox` / `PaintEventArgs` |
| `mainForm` 里 `welcomelabel` 覆盖整个棋盘 | `gaizi` 覆盖层在开局/结束时 `Show()` |
| `README` 只有一句话且以"、"结尾 | 原文如此，写于 2006 年，作者未写完 |
| `Backup/` 与 `Backup1/` 存在 | 修复过程中的手工快照，非项目组成部分 |
| `owlgame/bin/Debug/` 仍跟踪 exe 与 pdb | 老项目惯例，clone 下来双击即可运行；`obj/` 与 `vshost` 已移出（第 4 轮） |
