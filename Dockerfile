FROM microsoft/dotnet:1.1.1-sdk
RUN mkdir -p /dotnetapp/WukongNew/Wukong
WORKDIR /dotnetapp

COPY Wukong.sln /dotnetapp
COPY WukongNew /dotnetapp/Wukong
RUN dotnet restore -m
RUN dotnet build -m
