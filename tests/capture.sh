#!/usr/bin/env bash
# 重新生成 README 用的界面截图（输出到 img/）。
#
#   bash tests/capture.sh              # 默认输出到项目 img/
#   BUILD_DIR=/c/t_shots bash tests/capture.sh
#
# 与 run.sh 相同的两条构建约束：csc 不支持中文路径，且 Git Bash 会剥掉带目录
# 前缀的参数，所以源码先扁平拷进纯 ASCII 构建目录，再 cd 进去用 *.cs 通配。
#
# 需要交互式桌面会话：截图探针要调用 Form.Show()。
set -u

PROJ="$(cd "$(dirname "$0")/.." && pwd)"
BUILD="${BUILD_DIR:-/c/t_shots}"
CSC="/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
ICON="Core IP theme Icon 01.ico"
OUT="$PROJ/img"

if [ ! -x "$CSC" ]; then
    echo "找不到 csc.exe: $CSC" >&2
    exit 2
fi

echo "== 准备构建目录 $BUILD =="
rm -rf "$BUILD"
mkdir -p "$BUILD/Properties"
cp "$PROJ"/owlgame/*.cs            "$BUILD"/
cp "$PROJ"/owlgame/*.resx          "$BUILD"/
cp "$PROJ/owlgame/$ICON"           "$BUILD"/
cp "$PROJ"/owlgame/Properties/*.cs "$BUILD/Properties"/
cp "$PROJ/owlgame/Properties/Resources.resx" "$BUILD/Properties"/
cp "$PROJ"/tests/tools/CaptureScreenshots.cs "$BUILD/CaptureScreenshots.cs"

cd "$BUILD" || exit 2

echo "== 编译 owlgame.exe =="
MSYS_NO_PATHCONV=1 "$CSC" \
    -nologo -target:winexe -main:owlgame.Program \
    -out:owlgame.exe -debug -define:TRACE,DEBUG \
    -win32icon:"$ICON" \
    -resource:MainForm.resx,owlgame.MainForm.resources \
    -resource:gamehelp.resx,owlgame.gamehelp.resources \
    -resource:Properties/Resources.resx,owlgame.Properties.Resources.resources \
    *.cs || { echo "编译失败" >&2; exit 1; }

echo "== 编译截图探针 =="
MSYS_NO_PATHCONV=1 "$CSC" \
    -nologo -target:exe -out:Capture.exe -reference:owlgame.exe \
    CaptureScreenshots.cs || { echo "探针编译失败" >&2; exit 1; }

echo "== 生成截图 -> $OUT =="
mkdir -p "$OUT"
./Capture.exe "$OUT"
rc=$?

echo
echo "== img/ 内容 =="
ls -lh "$OUT" 2>/dev/null | tail -n +2
exit $rc
