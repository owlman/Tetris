# 回归探针

本项目没有单元测试框架，验证靠探针程序（各自带 `Main`，因此**不能**和 `owlgame/*.cs` 一起通配编译）。

| 文件 | 作用 |
| --- | --- |
| `Round1Regression.cs` | 逻辑回归：形状随机性、形状↔颜色映射、消行、边界拒绝、外链 scheme 校验、Game Over 后按钮状态。**第 3 轮起大部分断言直接驱动 `GameBoard`，不再靠反射** |
| `Round2Regression.cs` | 绘制回归：GDI 句柄稳定性、局部重绘是否真的局部、脏区是否覆盖旧+新位置、增量重绘与全绘的逐像素一致性 |
| `Round5Regression.cs` | 玩法回归：计分表、等级推进与乘数、下落间隔与下限、消行高亮状态机、分数读数 |
| `PixelSmoke.cs` | 像素冒烟：墙体像素守恒（闪烁检测）、当前块/预览框绘制、重玩路径 |

## 一键运行

```bash
bash tests/run.sh                  # 默认构建目录 C:\t_build
BUILD_DIR=/c/t2 bash tests/run.sh  # 自定义构建目录
```

脚本会：清空构建目录 → 拷源码（csc 不支持中文路径）→ 编译 owlgame.exe → 编译全部探针 → 逐个运行 → 汇总。任一步失败即返回非零。

## 手工运行

```bash
cd /c/t_build
MSYS_NO_PATHCONV=1 /c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe \
  /nologo /target:exe /out:probe_r2.exe /reference:owlgame.exe \
  "<项目路径>/tests/Round2Regression.cs"
./probe_r2.exe        # 结果同时写入 out_round2.txt（UTF-8），退出码 = 失败断言数
```

> 探针源码必须带 **UTF-8 BOM**，否则 `csc` 按 ANSI(936) 读会报一堆 `CS1513 / CS1519` 假语法错误。
> `csc` v4.0.30319 是 **C# 5** 编译器：本地函数、`?.`、`$"..."` 都不支持。探针曾因为写了本地函数 `Bitmap FullPaint()` 而编译失败。

## 基线

`PixelSmoke`（220×388 棋盘，`bw=11` / `bh=11`，**35 行**）：

```
after start  : gray(walls)=17200  colored(block)=400  navy=67760
120 key ops  : gray min=17200 max=17200   colored min=400 max=400
after restart: gray=17200 colored=400 navy=67760
preview      : corner=(128,128,128)  inside=(0,0,64)
```

**`gray` 一旦下降就代表擦除-重绘间隙出现，即闪烁回来了**；`colored` 下降代表方块没画出来。

`Round2Regression`：

```
GDI objects: before=22 after 20 restarts=22           ← 重开 20 次句柄数不增长
repaint rects: partial=79 full=0 (over 80 operations) ← 局部重绘生效，没有退化成全屏
dirty rect covers old+new block: 0 / 80 uncovered     ← 脏区包含旧位置 ∪ 新位置
ghost check: steps=80 stepsWithGhost=0 worstDiffPixels=0
```

`ghost check` 是残影的决定性判据：它把真实 Paint 事件给出的脏区矩形重放到一张**内存画布**（等价于双缓冲的后台缓冲），再与同状态的全量重绘逐像素比较。脏区若算少了，画布上会留下旧像素，差异数立刻不为 0。

> 为什么不抓屏幕：`CopyFromScreen` 依赖窗口焦点、DWM 合成时机与是否被遮挡，实测会因半合成帧（灰墙与背景按 24% 混合）和 ±1 色阶抖动产生大量假差异。内存画布方案完全确定性，可在无人值守环境下运行。
> 也不要拿 `DrawToBitmap` 当基准：它走 `WM_PRINT` 渲染路径，笔对齐与屏幕路径差 1 像素，会产生约 15000 个假差异。

`Round1Regression` 中的形状随机性断言允许少量碰撞（20 次抽样里约 2~3 对相同属正常随机），但修复前是 **20/20 全同**。

`Round5Regression` 的关键数值：

```
score for 1/2/3/4 line(s) = 100 / 300 / 500 / 800
level: 9 lines -> 1, 10 lines -> 2, level 2 doubles the points (delta = 200)
drop interval: level 1 = 1000 ms, level 2 = 910 ms, clamped at 120 ms
flash: gametimer_Tick leaves the board in "highlighting" state with flashTimer
       running; flashTimer_Tick clears the line and scores it
```

> 注意地板行不可消：`FindFullLines()` / `ClearLines()` 只检查 `i < lineCount - FloorThickness`。写探针填行时要用最后一个可消行（35 行棋盘是第 32 行），填地板行不会触发消行。
