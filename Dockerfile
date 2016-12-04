FROM microsoft/dotnet:1.1-sdk-msbuild
RUN mkdir -p /dotnetapp/src/Wukong

COPY Wukong.sln /dotnetapp
COPY src/Wukong/Wukong.csproj /dotnetapp/src/Wukong
RUN dotnet restore -m

COPY src/Wukong /dotnetapp/src/Wukong
RUN dotnet build -m
