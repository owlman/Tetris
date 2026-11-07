# 俄罗斯方块（Tetris）

> 作者：owlnan（浙江大学，2006 年）　技术栈：C# + Windows Forms（纯 GDI 运行时绘制，不使用位图资源）

一个用 C# + WinForms 实现的俄罗斯方块小游戏。棋盘、方块、预览框**全部由 GDI 在运行时绘制**，没有预置的位图素材；2026-11 定位并修复了按键时棋盘闪烁的老毛病。

---

## 玩法

| 按键 | 功能 |
| --- | --- |
| `↑` | 旋转当前方块 |
| `←` / `→` | 左右移动 |
| `↓` | 加速下落（一次下移 2 格） |

界面按钮：`开始` / `停止`、`重玩`、`说明`、`退出`。方块堆到顶部即游戏结束。

**消行规则**：一行中两侧墙之间的所有格子都被方块占满时，该行被消除，上方整体下移。

---

## 快速开始

### 直接运行

仓库中已跟踪编译好的可执行文件：

```
owlgame/bin/Debug/owlgame.exe
```

双击即可（Windows + .NET Framework 4.0 运行库）。

> ⚠️ 该 exe 是**被版本控制跟踪**的产物。改动源码后必须重新编译并覆盖它，否则仓库里的程序与源码不一致（见「构建」一节）。

### 从源码构建

本机**没有安装 .NET Framework SDK**，用 `MSBuild.exe` 构建会报：

```
error MSB3091: resgen.exe 未能找到 ... 未安装 .NET Framework SDK
```

因此直接调用 .NET Framework 4.0 自带的编译器 `csc.exe`：

```bash
# 1) 复制源码到纯 ASCII 路径（csc 不支持含中文的路径）
cd /c/t_build
cp "<项目路径>/owlgame/"*.cs .
cp "<项目路径>/owlgame/Properties/"*.cs Properties/
cp "<项目路径>/owlgame/"*.resx .
cp "<项目路径>/owlgame/Properties/Resources.resx" Properties/

# 2) 编译（注意 /main、三个 /resource、图标）
MSYS_NO_PATHCONV=1 /c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe \
  /nologo /target:winexe /main:owlgame.Program \
  /out:owlgame.exe /debug \
  /win32icon:"Core IP theme Icon 01.ico" \
  /resource:mainForm.resx,owlgame.mainForm.resources \
  /resource:gamehelp.resx,owlgame.gamehelp.resources \
  /resource:Properties/Resources.resx,owlgame.Properties.Resources.resources \
  *.cs

# 3) 回写被跟踪的产物
cp owlgame.exe "<项目路径>/owlgame/bin/Debug/owlgame.exe"
cp owlgame.pdb "<项目路径>/owlgame/bin/Debug/owlgame.pdb"
```

要点：

- `MSYS_NO_PATHCONV=1` 防止 Git Bash 把 `/out:` `/resource:` 当成路径转换掉。
- `/main:owlgame.Program` 必须给：目录里若残留带 `Main` 的探针文件会报 `CS0017: 程序集有多个入口点`。
- 三个 `.resx` 必须以**程序集限定名**嵌入，否则运行时报 `MissingManifestResourceException`。
- `csc` v4.0.30319 是 **C# 5** 编译器：不支持本地函数、`?.`、`$"..."` 插值等 C# 6+ 语法。

---

## 目录结构

```
Tetris/
├── owlgame/                    # 唯一的项目目录（项目名 MuscoviteBlock，程序集名 owlgame）
│   ├── MuscoviteBlock.csproj   # VS2005 时代的老式 csproj（Format 12.00，非 SDK-style）
│   ├── Program.cs              # 入口：EnableVisualStyles + Run(new mainForm())
│   ├── mainForm.cs             # 主窗体逻辑（自绘派发的唯一入口）
│   ├── mainForm.Designer.cs    # 主窗体布局（设计器生成）
│   ├── gamecontroller.cs       # 游戏核心：棋盘数据 + draw/drawwait 自绘
│   ├── block.cs                # 方块定义：7 种形状的 5×5 矩阵 + 随机取色
│   ├── gamehelp.cs(+Designer)  # 「说明」帮助窗体
│   ├── *.resx / Properties/    # 资源与程序集信息
│   └── bin/Debug/              # 已跟踪：owlgame.exe / owlgame.pdb
├── AGENTS.md                   # 面向 AI Agent 的项目工作指引（构建 / 编码 / 验证）
├── README.md                   # 本文件
└── owlgame.sln                 # 解决方案（已升级到 Format 12.00）
```

---

## 源码架构

三个类，一条数据流：

```
mainForm (UI + 事件)
   │  持 controller 引用；controller == null 表示「未开局」
   ├─ gamewiew_Paint(e)   ──► controller.draw(e)           画主棋盘（Panel 自绘）
   ├─ proimg_Paint(e)     ──► controller.drawwait(proimg,e) 画「下一个」预览（PictureBox 自绘）
   └─ gamewiew_PreviewKeyDown ─► controller.transpose() / move() / down()

gameController (纯逻辑 + 自绘)
   ├─ map : List<int[]>     棋盘数据，1=方块 2=墙 0=空
   ├─ now / wait : block    当前块 / 下一个块
   ├─ bw / bh : int         格子宽高，由 view 的宽高算出
   └─ draw() / drawwait()   两个 GDI 绘制入口

block (数据)
   └─ matblock[5,5] / curx / cury / colorblock
```

关键状态约定：

- **`controller == null` 是「未开局」哨兵**，所有 Paint 分支都靠它判断要不要画。
- `gaizi` 是覆盖在棋盘上的 `Dock=Fill` Panel，开局时 `Hide()`、结束时 `Show()` 显示「欢迎尝试游戏」/「你失败了…」。
- `gamewiew` 是 `Panel`（默认不开双缓冲），`proimg` 是 `PictureBox`（默认开双缓冲）。

---

## 已知问题与设计上的怪癖

| 现象 | 真相 |
| --- | --- |
| 棋盘是 20 列 × 38 行 | 列数固定 20，**行数由 `v.Height / v.Width * rowNo` 算出**（控件 220×388 → 38 行） |
| `block.size = 5` 但只有 7 种形状 | 5×5 矩阵里实际只占 2×4 / 3×2 等一小块，其余是 0 |
| 预览框位置看起来没居中 | `drawwait` 用 `box.Width/4 + j*bw` 定位，是硬编码的近似居中 |
| 没有分数、没有等级、没有消行动画 | 原作者未实现 |
| 游戏结束后「开始」按钮变灰 | 失败分支里 `buttonstart.Enabled = false`，此时只能用「重玩」 |
| `gamecontroller.cs` 里有 `using System.Windows.Forms` | 为了用 `Panel` / `PictureBox` / `PaintEventArgs` |

---

## 相关资料

- **实验笔记**：[《WinForms 自绘控件闪烁问题的定位与修复》](../../02.实验笔记/WinForms%20自绘控件闪烁问题的定位与修复.md) —— 闪烁问题的完整定位过程与像素级验证方法
- **姊妹实现**：[`../python/tetris.py`](../python/tetris.py) —— Python + pygame 版，与 C# 版互不引用
- **Agent 工作指引**：[`AGENTS.md`](AGENTS.md) —— 构建流程、编码规范、验证方法、踩坑清单

---

## 原始 README（2006 年作者原文）

> 这是一个用 c# 实现的俄罗斯方块小游戏。由于实现中没有采用位图缓存，而是运行时旋转生成，画面有些闪……

*（原文写于 2006 年，作者未写完。）*
