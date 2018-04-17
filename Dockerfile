FROM microsoft/dotnet:1.1.8-sdk-1.1.9 AS build-env
WORKDIR /dotnetapp
COPY Wukong.sln .
COPY Wukong ./Wukong
COPY Wukong.Tests ./Wukong.Tests

RUN dotnet restore
RUN dotnet publish Wukong/Wukong.csproj -c Debug -o out

# runtime image
FROM microsoft/dotnet:1.1.8-runtime
WORKDIR /dotnetapp
COPY --from=build-env /dotnetapp/Wukong/out .
VOLUME [ "/dotnetapp/runtime" ]
RUN apt-get update
RUN apt-get install -y curl unzip
RUN curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l ~/vsdbg
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 5000
ENTRYPOINT [ "dotnet", "Wukong.dll" ]
