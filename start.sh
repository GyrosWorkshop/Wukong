#!/bin/bash
set -e

echo "Use wukong-provider: ${WUKONG_PROVIDER}"
cp appsettings.template.json appsettings.json
WUKONG_PROVIDER_ESCAPE=${WUKONG_PROVIDER//\//\\\/}
sed -i "s/\"WUKONG_PROVIDER\"/\"${WUKONG_PROVIDER_ESCAPE}\"/" appsettings.json
dotnet run -p WukongNew/Wukong.csproj -c RELEASE
