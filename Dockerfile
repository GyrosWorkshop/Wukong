FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /dotnetapp
COPY Wukong.sln .
COPY Wukong ./Wukong
COPY Wukong.Tests ./Wukong.Tests

RUN dotnet restore
RUN dotnet publish Wukong/Wukong.csproj -c Debug -o out

# runtime image
FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /dotnetapp
COPY --from=build-env /dotnetapp/Wukong/out .
VOLUME [ "/dotnetapp/runtime" ]
EXPOSE 80
ENTRYPOINT [ "dotnet", "Wukong.dll" ]
