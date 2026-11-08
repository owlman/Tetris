#!/usr/bin/env bash
# 编译 owlgame + 单元测试 + 像素探针，并跑全部检查。
#
#   bash tests/run.sh                     # 单元测试（含 UI）+ 像素探针
#   bash tests/run.sh --skip-ui           # 跳过需要桌面会话的 UI 测试
#   bash tests/run.sh --filter Board      # 只跑名字含 Board 的用例
#   bash tests/run.sh --list              # 只列出用例名
#   BUILD_DIR=/c/t2 bash tests/run.sh     # 自定义构建目录
#
# 两个必须遵守的构建约束：
#   1. csc.exe 不支持含中文的路径，而本仓库路径里有「游戏开发」，所以源码先
#      拷到纯 ASCII 的构建目录。
#   2. Git Bash 调用原生 exe 时会把带目录前缀的参数剥掉（`tests/foo.cs` 到
#      csc 眼里只剩 `foo.cs`），而 MSYS_NO_PATHCONV=1 也救不回来。对策是把
#      待编译的源码扁平拷进一个子目录，`cd` 进去用 `*.cs` 通配 —— 没有目录
#      前缀就不会被剥。
set -u

PROJ="$(cd "$(dirname "$0")/.." && pwd)"
BUILD="${BUILD_DIR:-/c/t_build}"
CSC="/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
ICON="Core IP theme Icon 01.ico"

# 透传给单元测试运行器的参数
RUNNER_ARGS=("$@")

red()   { printf '\033[31m%s\033[0m\n' "$*"; }
green() { printf '\033[32m%s\033[0m\n' "$*"; }
bold()  { printf '\033[1m%s\033[0m\n' "$*"; }

if [ ! -x "$CSC" ]; then
    red "找不到 csc.exe: $CSC"
    exit 2
fi

bold "== 1/5 准备构建目录 $BUILD =="
rm -rf "$BUILD"
mkdir -p "$BUILD/Properties" "$BUILD/tests-src"
cp "$PROJ"/owlgame/*.cs            "$BUILD"/
cp "$PROJ"/owlgame/*.resx          "$BUILD"/
cp "$PROJ/owlgame/$ICON"           "$BUILD"/
cp "$PROJ"/owlgame/Properties/*.cs "$BUILD/Properties"/
cp "$PROJ/owlgame/Properties/Resources.resx" "$BUILD/Properties"/
echo "   owlgame 源码 $(ls "$BUILD"/*.cs | wc -l) 个 .cs"

bold "== 2/5 编译 owlgame.exe =="
cd "$BUILD" || exit 2
# 用 `-` 前缀的选项：csc 同时接受 / 和 -，而 - 不会被 Git Bash 当路径处理。
MSYS_NO_PATHCONV=1 "$CSC" \
    -nologo -target:winexe -main:owlgame.Program \
    -out:owlgame.exe -debug -define:TRACE,DEBUG \
    -win32icon:"$ICON" \
    -resource:MainForm.resx,owlgame.MainForm.resources \
    -resource:gamehelp.resx,owlgame.gamehelp.resources \
    -resource:Properties/Resources.resx,owlgame.Properties.Resources.resources \
    *.cs
if [ $? -ne 0 ]; then
    red "编译失败"
    exit 1
fi
green "   owlgame.exe OK"

bold "== 3/5 编译单元测试 =="
cp "$PROJ"/tests/framework/*.cs   "$BUILD/tests-src"/
cp "$PROJ"/tests/TetrisTests/*.cs "$BUILD/tests-src"/
cd "$BUILD/tests-src" || exit 2
MSYS_NO_PATHCONV=1 "$CSC" \
    -nologo -target:exe -out:../TetrisTests.exe -debug -define:TRACE,DEBUG \
    -reference:../owlgame.exe \
    *.cs
if [ $? -ne 0 ]; then
    red "单元测试编译失败"
    exit 1
fi
green "   TetrisTests.exe OK"

bold "== 4/5 运行单元测试 =="
cd "$BUILD" || exit 2
./TetrisTests.exe "${RUNNER_ARGS[@]}"
unit_rc=$?
if [ $unit_rc -ne 0 ]; then
    red "   单元测试失败（$unit_rc 个用例未通过）"
else
    green "   单元测试全部通过"
fi

bold "== 5/5 运行像素探针 =="
# 这两个探针保留为独立程序：它们输出的是可对比的像素基线数字
# （墙体像素数、逐像素差异），不是 PASS/FAIL 型的用例。
probe_rc=0
for src in "$PROJ"/tests/probes/*.cs; do
    [ -e "$src" ] || continue
    name="$(basename "$src" .cs)"
    cp "$src" "$BUILD/probes-src-$name.cs"
    MSYS_NO_PATHCONV=1 "$CSC" \
        -nologo -target:exe -out:"$name.exe" -reference:owlgame.exe "probes-src-$name.cs"
    if [ $? -ne 0 ]; then
        red "   $name 编译失败"
        probe_rc=1
        continue
    fi
    echo "   --- $name ---"
    ./"$name.exe"
    if [ $? -ne 0 ]; then
        red "   $name 失败"
        probe_rc=1
    else
        green "   $name 通过"
    fi
done

echo
for f in "$BUILD"/out_*.txt; do
    [ -e "$f" ] || continue
    echo "=== $(basename "$f") ==="
    cat "$f"
    echo
done

if [ $unit_rc -eq 0 ] && [ $probe_rc -eq 0 ]; then
    green "全部检查通过"
    exit 0
fi
red "存在失败的检查"
exit 1
