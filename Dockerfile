FROM microsoft/dotnet:1.0.0-preview2-sdk
RUN mkdir -p /dotnetapp
COPY src/Wukong /dotnetapp
WORKDIR /dotnetapp
RUN dotnet restore
RUN dotnet build