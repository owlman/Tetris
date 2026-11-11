#!/usr/bin/env bash
# 编译发布版 exe，并回写被跟踪的 owlgame/bin/Debug/owlgame.exe。
#
# 一次产出三份，语言由编译期常量决定（运行期仍可用 --lang=xx 覆盖）：
#   owlgame.exe     不定义 LANG_*，界面语言跟随操作系统 UI 文化
#   owlgame-zh.exe  /define:LANG_ZH，无论系统语言都是中文
#   owlgame-en.exe  /define:LANG_EN，无论系统语言都是英文
#
# 用法：bash tests/build-release.sh
set -u

PROJ="D:/Working/writing/0_LabNotes/游戏开发/01.实验项目/C#/Tetris"
BUILD="/c/t_rel"
CSC="/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
ICON="Core IP theme Icon 01.ico"

rm -rf "$BUILD"
mkdir -p "$BUILD/Properties" || exit 2

cp "$PROJ"/owlgame/*.cs            "$BUILD"/
cp "$PROJ"/owlgame/*.resx          "$BUILD"/
cp "$PROJ/owlgame/$ICON"           "$BUILD"/
cp "$PROJ"/owlgame/Properties/*.cs "$BUILD/Properties"/
cp "$PROJ/owlgame/Properties/Resources.resx" "$BUILD/Properties"/

cd "$BUILD" || exit 2
echo "源码 $(ls *.cs | wc -l) 个 .cs"

# build <输出名> <额外的编译期常量，可为空>
build() {
    local out="$1" extra="$2"
    local defs="TRACE,DEBUG"
    [ -n "$extra" ] && defs="$defs,$extra"
    echo "--- $out  (-define:$defs)"
    MSYS_NO_PATHCONV=1 "$CSC" \
        -nologo -target:winexe -main:owlgame.Program \
        -out:"$out" -debug -define:"$defs" \
        -win32icon:"$ICON" \
        -resource:MainForm.resx,owlgame.MainForm.resources \
        -resource:gamehelp.resx,owlgame.gamehelp.resources \
        -resource:Properties/Resources.resx,owlgame.Properties.Resources.resources \
        *.cs || { echo "编译失败：$out"; exit 1; }
}

build owlgame.exe    ""
build owlgame-zh.exe "LANG_ZH"
build owlgame-en.exe "LANG_EN"

cp owlgame.exe    "$PROJ/owlgame/bin/Debug/owlgame.exe"    || exit 1
cp owlgame.pdb    "$PROJ/owlgame/bin/Debug/owlgame.pdb"    || exit 1
cp owlgame-zh.exe "$PROJ/owlgame/bin/Debug/owlgame-zh.exe" || exit 1
cp owlgame-en.exe "$PROJ/owlgame/bin/Debug/owlgame-en.exe" || exit 1

echo
for f in owlgame.exe owlgame-zh.exe owlgame-en.exe; do
    printf '%-16s %8d 字节\n' "$f" "$(stat -c%s "$f")"
done
echo "已回写 owlgame/bin/Debug/（4 个文件）"
