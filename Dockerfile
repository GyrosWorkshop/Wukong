FROM microsoft/dotnet:1.0.0-preview2-sdk
RUN mkdir -p /dotnetapp
COPY src/Wukong /dotnetapp
WORKDIR /dotnetapp
ONBUILD RUN dotnet restore
ONBUILD RUN dotnet build