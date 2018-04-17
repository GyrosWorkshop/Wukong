FROM microsoft/dotnet:1.1.8-sdk-1.1.9 AS build-env
WORKDIR /dotnetapp
COPY Wukong.sln .
COPY Wukong ./Wukong
COPY Wukong.Tests ./Wukong.Tests

RUN dotnet restore
RUN dotnet publish Wukong/Wukong.csproj -c Release -o out

# runtime image
FROM microsoft/dotnet:1.1.8-runtime
WORKDIR /dotnetapp
COPY --from=build-env /dotnetapp/Wukong/out .

EXPOSE 5000
ENTRYPOINT [ "dotnet", "Wukong.dll" ]
