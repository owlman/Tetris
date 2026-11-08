# AGENTS.md

本文件是《Tetris / 俄罗斯方块》（C# + WinForms）实验项目的 Agent 工作指引。

项目路径：`D:\Working\writing\0_LabNotes\游戏开发\01.实验项目\C#/Tetris\`

---

## 项目是什么

一个 2006 年浙大（zju）作者用 C# + WinForms 写的俄罗斯方块小游戏，**画面全部由 GDI 手工绘制**，而不是用控件摆放方块。原始版本存在"按键操作时画面闪烁"的缺陷，2026-11 已定位并修复（详见"闪烁修复"一节与配套实验笔记）。

- 玩法：方向键 ↑ 旋转、← → 移动、↓ 加速下落；「开始」启动、「重玩」重置、「说明」看帮助。
- 棋盘 20 列 × 约 38 行（行数由控件宽高比算出），7 种方块、3 种颜色。
- 同一 `游戏开发` 目录下还有一个姊妹实现：`../python/tetris.py`（Python + pygame 版），两者互不引用。

配套实验笔记（**改动本项目时应同步考虑是否需要更新**）：
`../../02.实验笔记/C# 版俄罗斯方块 bug 修复之WinForms 自绘控件闪烁问题的定位与修复.md`

---

## 仓库拓扑（重要，先搞清楚再动手）

本项目是**嵌套在另一个 git 仓库里的独立 git 仓库**：

| 层 | 路径 | 仓库 | remote | 分支 |
| --- | --- | --- | --- | --- |
| 内层（本项目） | `.../C#/Tetris/` | 独立 git 仓 | `origin` → `https://gitee.com/owlman/owlgame-Tetris` | `master` |
| 外层（笔记库） | `0_LabNotes/` | 独立 git 仓 | **无 remote** | `master` |
| 更外层 | `D:\Working\writing\` | **不是 git 仓** | — | — |

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
│   ├── Program.cs           # 入口：EnableVisualStyles + Run(new mainForm())
│   ├── mainForm.cs          # 主窗体逻辑（自绘派发的唯一入口）
│   ├── mainForm.Designer.cs # 主窗体布局（设计器生成，勿手改）
│   ├── gamecontroller.cs    # 游戏核心：棋盘数据 + draw/drawwait 自绘
│   ├── block.cs             # 方块定义：7 种形状的 5×5 矩阵 + 随机取色
│   ├── gamehelp.cs(+Designer) # 「说明」帮助窗体
│   ├── mainForm.resx / gamehelp.resx
│   ├── Properties/          # AssemblyInfo / Resources / Settings
│   ├── bin/Debug/           # **已被 git 跟踪**，含 owlgame.exe / .pdb / .vshost.exe
│   └── obj/                 # **部分被 git 跟踪**（旧 csproj 的产物）
├── Backup/                  # 未跟踪：修复前的原始代码快照
├── Backup1/                 # 未跟踪：闪烁修复后的中间快照
├── temp/                    # 未跟踪：临时产物（backup_bin/ 存原版 exe）
├── temp_build/              # 未跟踪：空的构建暂存目录
├── .vs/                     # 未跟踪：VS 2026 本地状态
├── README                   # 项目简介（UTF-8 BOM）
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
  /resource:mainForm.resx,owlgame.mainForm.resources \
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
| `README` | UTF-8 | 有 | LF |

`.gitattributes` 里写了 `* -text`，禁用 git 的行尾自动转换 —— 仓库中保存的字节与工作区**完全一致**，避免在 Windows / Linux 之间同步时出现整文件行尾 diff。

> ⚠️ **带 BOM 是刻意的**：`csc.exe` 与旧版 MSBuild 读无 BOM 的源文件时按系统 ANSI 代码页（936）解释，UTF-8 中文会直接编译成乱码；带 BOM 后编译器能无歧义地按 UTF-8 读取。

规则：

1. 改**任何** `.cs` 一律按 **UTF-8 with BOM + CRLF** 读写。
2. Python 脚本读写时用 `encoding='utf-8-sig'`（读）/ `encoding='utf-8-sig'`（写）+ `newline='\r\n'`，并用默认 `newline=None` 归一化行尾（用 `newline=''` 保留 `\r\n` 会导致用 `\n` 构造的匹配串**永远匹配不上**）。
3. **新增注释一律写英文**，可以顺带规避整个编码问题。
4. 改写前确认文件在版本控制里（`git status` + 文件大小自检），出事可 `git checkout HEAD -- <path>` 恢复。

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

### 三个类，一条数据流

```
mainForm (UI + 事件)
   │  持 controller 引用；controller == null 表示"未开局"
   ├─ gamewiew_Paint(e)  ──► controller.draw(e)          画主棋盘（Panel 自绘）
   ├─ proimg_Paint(e)    ──► controller.drawwait(proimg,e) 画"下一个"预览（PictureBox 自绘）
   └─ gamewiew_PreviewKeyDown ─► controller.transpose()/move()/down()

gameController (纯逻辑 + 自绘)
   ├─ map : List<int[]>     棋盘数据，1=方块 2=墙 0=空
   ├─ now / wait : block    当前块 / 下一个块
   ├─ bw / bh : int         格子宽高，由 view 的宽高算出
   └─ draw() / drawwait()   两个 GDI 绘制入口

block (数据)
   └─ matblock[5,5] / curx / cury / colorblock
```

### 关键状态约定

- **`controller == null` 是"未开局"哨兵**。`mainForm` 里所有 Paint 分支都靠它判断要不要画。改了 Paint 逻辑要连带检查这个分支（见下面 proimg 的教训）。
- `gamewiew` 是 `Panel`，**默认不开双缓冲**；`proimg` 是 `PictureBox`，**默认开双缓冲**。这是实测结论。
- `gaizi` 是覆盖在 `gamewiew` 上的 `Dock=Fill` Panel，开局时 `Hide()`、结束时 `Show()` 显示"欢迎尝试游戏"/"游戏失败啦"。
- `gamewiew.AutoSize` 已从 `true` 改为 `false`（原本与内部 `Dock=Fill` 子控件形成递归布局约束）。

### 闪烁修复（已落地，不要回退）

原问题：按键时棋盘闪烁。定位到三个根因——擦除-重绘间隙、脏矩形偏移写错（`(j + now.curx - 4) * bw` 多减了 4 格）、`AutoSize` 与 `Dock=Fill` 递归约束。

已实施的修复：

1. `EnableDoubleBuffer()` —— 反射设置 `Control.DoubleBuffered`，**递归到所有子控件**。
2. `ApplyFlickerFix()` —— 反射 `SetStyle` 追加 `OptimizedDoubleBuffer | AllPaintingInWmPaint | Opaque`，断掉 `WM_ERASEBKGND`。
3. **`Opaque` 是一个契约**：声明"我覆盖每一个像素"，因此**自绘函数必须自己铺满整个背景**。`draw()` / `drawwait()` 开头都加了 `FillRectangle(bgBrush, ClientRectangle)`。
4. `Invalidateblock()` 改为整块 `view.Invalidate()`（绕开脏矩形偏移 bug）。
5. `gamewiew.AutoSize = false`。

> ⚠️ **改动自绘函数时必须记住 Opaque 契约**：任何一个 Paint 分支"什么都不画"，屏幕就会残留脏像素。
> 实例：`proimg_Paint` 原本是 `if (controller != null) controller.drawwait(...)`，初始界面 `controller == null` → 一帧不画 → 因为 `proimg` 带 `Opaque` 标志、Windows 不再替它擦背景 → **「下一个」下面那块预览窗格整个消失**。修法是先无条件铺 `proimg.BackColor`，再画预览，最后补一圈灰色边框。改动此函数时务必覆盖"未开局"与"游戏中"两个状态。

---

## 验证方法（本项目没有单元测试，靠探针）

> 📁 **回归探针已固化**：`tests/Round1Regression.cs`（逻辑）与 `tests/PixelSmoke.cs`（像素），
> 用法与基线见 `tests/README.md`。探针各自带 `Main`，**不要**与 `owlgame/*.cs` 一起通配编译。

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

## Git 约定

- 内层仓库 `master` 分支，remote `origin` → gitee `owlman/owlgame-Tetris`（已验证可连通）。
- 提交前跑 `git status -sb`，注意 `owlgame/bin/Debug/owlgame.exe`、`owlgame.pdb` 是被跟踪的二进制，**源码改动要一并重新编译并提交**（否则仓库里的 exe 与源码不一致，下次别人运行还是旧行为）。
- commit message 遵循 Conventional Commits；仓库历史是英文短句风格（`first commit` / `add README` / `this is a game` / `vs 2008 file...`），新提交建议用 `fix:` / `chore:` / `docs:` 等标准前缀。
- **不要**主动推送外层 `0_LabNotes` 仓库（它没有 remote，且 `游戏开发/` 目录当前是未跟踪状态，是否纳入由用户决定）。
- 文件重命名一律 `git mv -f`（Windows 大小写不敏感，`rm -f` + `git add` 会删掉唯一副本）。

---

## 已知的"看起来像 bug 但其实是设计"的地方

| 现象 | 真相 |
| --- | --- |
| `gamecontroller` 里 `rowNo = 20` 但棋盘是 20×38 | 列数固定 20，**行数由 `v.Height / v.Width * rowNo` 算出**（220×388 的控件 → 38 行） |
| `drawwait` 里用 `box.Width/4 + j*bw` 定位预览 | 预览框比棋盘小得多，位置是硬编码的近似居中，不是数学居中 |
| `block.size = 5` 但只有 7 种形状 | 5×5 矩阵里实际只占 2×4 / 3×2 等一小块，其余是 0 |
| `gamecontroller.cs` 里有 `using System.Windows.Forms` | 为了用 `Panel` / `PictureBox` / `PaintEventArgs` |
| `mainForm` 里 `welcomelabel` 覆盖整个棋盘 | `gaizi` 覆盖层在开局/结束时 `Show()` |
| `README` 只有一句话且以"、"结尾 | 原文如此，写于 2006 年，作者未写完 |
| `Backup/` 与 `Backup1/` 存在 | 修复过程中的手工快照，非项目组成部分 |
