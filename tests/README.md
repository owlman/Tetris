# 回归探针

本项目没有单元测试框架，验证靠两个独立的探针程序（各自带 `Main`，因此**不能**和 `owlgame/*.cs` 一起通配编译）。

| 文件 | 作用 |
| --- | --- |
| `Round1Regression.cs` | 逻辑回归：形状随机性、形状↔颜色映射、外链 scheme 校验、Game Over 后按钮状态 |
| `PixelSmoke.cs` | 像素冒烟：墙体像素守恒（闪烁检测）、当前块/预览框绘制、重玩路径 |

## 用法

```bash
# 1) 先把 owlgame 编译到纯 ASCII 路径（见 AGENTS.md「构建」一节）
cd /c/t_build

# 2) 编译探针（探针文件必须带 UTF-8 BOM，否则 csc 按 ANSI 读到一堆假语法错误）
MSYS_NO_PATHCONV=1 /c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe \
  /nologo /target:exe /out:probe_r1.exe /reference:owlgame.exe \
  "<项目路径>/tests/Round1Regression.cs"

MSYS_NO_PATHCONV=1 /c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe \
  /nologo /target:exe /out:probe_smoke.exe /reference:owlgame.exe \
  "<项目路径>/tests/PixelSmoke.cs"

# 3) 运行；结果同时写入 out_*.txt（UTF-8），退出码为失败断言数
./probe_r1.exe
./probe_smoke.exe
```

## 基线

`PixelSmoke` 的实测基线（220×388 棋盘，`bw=11` / `bh=10`，38 行）：

```
after start  : gray(walls)=17200  colored(block)=400  navy=67760
120 key ops  : gray min=17200 max=17200   colored min=400 max=400
after restart: gray=17200 colored=400 navy=67760
preview      : corner=(128,128,128)  inside=(0,0,64)
```

**`gray` 一旦下降就代表擦除-重绘间隙出现，即闪烁回来了**；`colored` 下降代表方块没画出来。

`Round1Regression` 中的形状随机性断言允许少量碰撞（20 次抽样里约 2~3 对相同属正常随机），但修复前是 **20/20 全同**。
