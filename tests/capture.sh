#!/usr/bin/env bash
# 重新生成 README 用的界面截图。
#
#   bash tests/capture.sh              # 中文界面 -> img/
#   bash tests/capture.sh en           # 英文界面 -> img/en/
#   BUILD_DIR=/c/t_shots bash tests/capture.sh en
#
# 语言由环境变量 OWLGAME_LANG 传给探针（Strings.Configure 的解析链会读它）。
# 方块序列由 OWLGAME_SEED 固定（默认 20261111），所以重复运行得到的 PNG 逐字节
# 相同，不会因为随机方块而每次提交都产生二进制噪音。
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

# 语言选择：第一个位置参数优先，其次 OWLGAME_LANG，默认中文。
LANG_CODE="${1:-${OWLGAME_LANG:-zh}}"
case "$LANG_CODE" in
    en|EN|en-US|English|english)
        LANG_CODE=en
        OUT="$PROJ/img/en"
        ;;
    *)
        LANG_CODE=zh
        OUT="$PROJ/img"
        ;;
esac
export OWLGAME_LANG="$LANG_CODE"
export OWLGAME_SEED="${OWLGAME_SEED:-20261111}"

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

echo "== 生成截图（$LANG_CODE） -> $OUT =="
mkdir -p "$OUT"
./Capture.exe "$OUT"
rc=$?

echo
echo "== img/ 内容 =="
ls -lh "$OUT" 2>/dev/null | tail -n +2
exit $rc
