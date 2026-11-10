#!/usr/bin/env bash
# 把改动后的源码重新编译成被跟踪的 owlgame/bin/Debug/owlgame.exe。
# 与 tests/run.sh 用同一套 csc 参数（三个 resx 必须按程序集限定名嵌入）。
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

MSYS_NO_PATHCONV=1 "$CSC" \
    -nologo -target:winexe -main:owlgame.Program \
    -out:owlgame.exe -debug -define:TRACE,DEBUG \
    -win32icon:"$ICON" \
    -resource:MainForm.resx,owlgame.MainForm.resources \
    -resource:gamehelp.resx,owlgame.gamehelp.resources \
    -resource:Properties/Resources.resx,owlgame.Properties.Resources.resources \
    *.cs || { echo "编译失败"; exit 1; }

cp owlgame.exe owlgame.pdb "$PROJ/owlgame/bin/Debug/" || exit 1
echo "已回写 bin/Debug/owlgame.exe ($(stat -c%s owlgame.exe) 字节)"
