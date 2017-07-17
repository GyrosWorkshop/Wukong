FROM microsoft/dotnet:1.1.1-runtime-deps
LABEL maintainer="Senorsen <senorsen.zhang@gmail.com>"
WORKDIR /dotnetapp
RUN mkdir -p /dotnetapp

ADD dotnetapp/wukong-dist_linux-x64.tar.gz .

EXPOSE 5000
CMD ["./wukong-dist_linux-x64/Wukong"]
