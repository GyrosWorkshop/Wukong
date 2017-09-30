#!/usr/bin/env bash
set -e
cd Wukong
dotnet restore
dotnet build
