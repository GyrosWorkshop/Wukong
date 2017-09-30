#!/usr/bin/env bash
set -e
cd Wukong
dotnet restore -r linux-x64 -r win-x64 -r win-x86
dotnet build
