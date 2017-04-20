FROM microsoft/dotnet:1.1.1-runtime-deps
LABEL maintainer="Senorsen <senorsen.zhang@gmail.com>"
WORKDIR /dotnetapp
RUN mkdir -p /dotnetapp

ADD dotnetapp/wukong-dist_linux-x64.tar.gz .
COPY appsettings.template.json Wukong/appsettings.json ./

COPY docker-entrypoint.sh /usr/local/bin/
RUN ln -s usr/local/bin/docker-entrypoint.sh / # backwards compat
ENTRYPOINT ["docker-entrypoint.sh"]
VOLUME /dotnetapp/wukong-dist_linux-x64/database
EXPOSE 5000
CMD ["./wukong-dist_linux-x64/Wukong"]
