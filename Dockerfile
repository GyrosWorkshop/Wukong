FROM microsoft/dotnet:1.1.1-sdk
RUN mkdir -p /dotnetapp
WORKDIR /dotnetapp

COPY Wukong.sln /dotnetapp
COPY WukongNew /dotnetapp/WukongNew
RUN dotnet restore -m
RUN dotnet build -m
