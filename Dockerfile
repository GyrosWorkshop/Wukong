FROM microsoft/dotnet:1.1-sdk-msbuild
RUN mkdir -p /dotnetapp/WukongNew/Wukong
WORKDIR /dotnetapp

COPY Wukong.sln /dotnetapp
COPY WukongNew/Wukong/Wukong.csproj /dotnetapp/WukongNew/Wukong
RUN dotnet restore -m

COPY WukongNew/Wukong /dotnetapp/WukongNew/Wukong
RUN dotnet build -m
