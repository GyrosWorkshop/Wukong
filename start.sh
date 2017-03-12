#!/bin/bash
set -e

echo "Use wukong-provider: ${WUKONG_PROVIDER}"
cp appsettings.template.json appsettings.json
sed -i "s/\"APPLICATION_INSIGHTS_INSTRUMENTATION_KEY\"/\"${APPLICATION_INSIGHTS_INSTRUMENTATION_KEY}\"/" appsettings.json
sed -i "s/\"GOOGLE_CLIENT_ID\"/\"${GOOGLE_CLIENT_ID}\"/" appsettings.json
sed -i "s/\"GOOGLE_CLIENT_SECRET\"/\"${GOOGLE_CLIENT_SECRET}\"/" appsettings.json
sed -i "s/\"GITHUB_CLIENT_ID\"/\"${GITHUB_CLIENT_ID}\"/" appsettings.json
sed -i "s/\"GITHUB_CLIENT_SECRET\"/\"${GITHUB_CLIENT_SECRET}\"/" appsettings.json
WUKONG_PROVIDER_ESCAPE=${WUKONG_PROVIDER//\//\\\/}
sed -i "s/\"WUKONG_PROVIDER\"/\"${WUKONG_PROVIDER_ESCAPE}\"/" appsettings.json
dotnet run -p WukongNew/Wukong.csproj -c RELEASE
