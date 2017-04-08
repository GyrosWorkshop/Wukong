FROM microsoft/dotnet:1.1.1-runtime-deps
LABEL maintainer="Senorsen <senorsen.zhang@gmail.com>"
WORKDIR /dotnetapp
RUN mkdir -p /dotnetapp

COPY wukong-dist_linux-x64 ./wukong-dist_linux-x64

COPY start.sh /
RUN chmod +x /start.sh
COPY appsettings.template.json .

EXPOSE 5000
CMD ["/start.sh"]
