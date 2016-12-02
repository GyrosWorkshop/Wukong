#!/bin/sh
set -e

echo "Use wukong-provider: ${WUKONG_PROVIDER}"
sed -i "s/\"WUKONG_PROVIDER\"/\"${WUKONG_PROVIDER}\"/" appsettings.json
dotnet run
