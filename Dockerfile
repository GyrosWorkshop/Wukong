FROM microsoft/dotnet:1.1.4-runtime-deps
LABEL maintainer="Senorsen <senorsen.zhang@gmail.com>"
WORKDIR /dotnetapp
RUN mkdir -p /dotnetapp

ADD dotnetapp/wukong-linux-x64.tar.gz .

EXPOSE 5000
CMD ["./wukong-linux-x64/Wukong"]
