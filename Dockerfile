FROM microsoft/dotnet:1.1.1-sdk
WORKDIR /dotnetapp

COPY Wukong.sln /dotnetapp/
COPY WukongNew /dotnetapp/WukongNew/
RUN dotnet restore -m
RUN dotnet build -m

COPY appsettings.template.json /dotnetapp/

COPY start.sh /
RUN chmod +x /start.sh

EXPOSE 5000
CMD /start.sh
