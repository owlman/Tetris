#!/usr/bin/env bash
# 一键回归：编译 owlgame 与全部探针，跑断言，汇总结果。
#
# 用法：  bash tests/run.sh            # 默认构建目录 C:\t_build
#         BUILD_DIR=/c/t2 bash tests/run.sh
#
# 为什么要把源码拷到 C:\t_build：csc.exe 不支持含中文的路径，
# 而本仓库路径里有「游戏开发」。MSYS_NO_PATHCONV=1 则是防止 Git Bash
# 把 /out: /resource: 这类参数当成路径做转换。
set -u

PROJ="$(cd "$(dirname "$0")/.." && pwd)"
BUILD="${BUILD_DIR:-/c/t_build}"
CSC="/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
ICON="Core IP theme Icon 01.ico"

red()   { printf '\033[31m%s\033[0m\n' "$*"; }
green() { printf '\033[32m%s\033[0m\n' "$*"; }
bold()  { printf '\033[1m%s\033[0m\n' "$*"; }

if [ ! -x "$CSC" ]; then
    red "找不到 csc.exe: $CSC"
    exit 2
fi

bold "== 1/4 准备构建目录 $BUILD =="
rm -rf "$BUILD"
mkdir -p "$BUILD/Properties"
cp "$PROJ"/owlgame/*.cs            "$BUILD"/
cp "$PROJ"/owlgame/*.resx          "$BUILD"/
cp "$PROJ/owlgame/$ICON"           "$BUILD"/
cp "$PROJ"/owlgame/Properties/*.cs "$BUILD/Properties"/
cp "$PROJ"/owlgame/Properties/Resources.resx "$BUILD/Properties"/
echo "   源码 $(ls "$BUILD"/*.cs | wc -l) 个 .cs"

bold "== 2/4 编译 owlgame.exe =="
cd "$BUILD" || exit 2
MSYS_NO_PATHCONV=1 "$CSC" \
    /nologo /target:winexe /main:owlgame.Program \
    /out:owlgame.exe /debug /define:TRACE,DEBUG \
    /win32icon:"$ICON" \
    /resource:MainForm.resx,owlgame.MainForm.resources \
    /resource:gamehelp.resx,owlgame.gamehelp.resources \
    /resource:Properties/Resources.resx,owlgame.Properties.Resources.resources \
    *.cs
if [ $? -ne 0 ]; then
    red "编译失败"
    exit 1
fi
green "   owlgame.exe OK"

bold "== 3/4 编译探针 =="
PROBES=()
for src in "$PROJ"/tests/*.cs; do
    [ -e "$src" ] || continue
    name="$(basename "$src" .cs)"
    cp "$src" "$BUILD/$name.cs"
    MSYS_NO_PATHCONV=1 "$CSC" \
        /nologo /target:exe /out:"$name.exe" /reference:owlgame.exe "$name.cs"
    if [ $? -ne 0 ]; then
        red "   $name 编译失败"
        exit 1
    fi
    PROBES+=("$name")
    green "   $name.exe OK"
done

bold "== 4/4 运行 =="
fail=0
for name in "${PROBES[@]}"; do
    echo "   --- $name ---"
    # Give the previous probe's window time to close, otherwise the next one
    # may never get foreground and its screen-capture checks degrade to SKIP.
    sleep 1
    ./"$name.exe"
    rc=$?
    if [ $rc -ne 0 ]; then
        red "   $name 失败（退出码 $rc）"
        fail=$((fail + 1))
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

if [ $fail -eq 0 ]; then
    green "全部探针通过"
    exit 0
else
    red "$fail 个探针失败"
    exit 1
fi
