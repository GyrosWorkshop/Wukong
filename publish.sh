#!/bin/sh

CSPROJ=Wukong/Wukong.csproj
CONFIGURATION=Release
OUTPUT_PREFIX=Wukong

for rid in $*; do
    OUTPUT="$OUTPUT_PREFIX-$rid"
    dotnet publish $CSPROJ -c Release -o $OUTPUT -r $rid
    tar czvf $OUTPUT.tar.gz $OUTPUT
done
