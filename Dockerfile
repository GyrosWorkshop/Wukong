FROM microsoft/dotnet:1.1.1-runtime-deps
LABEL maintainer="Senorsen <senorsen.zhang@gmail.com>"
WORKDIR /dotnetapp
RUN mkdir -p /dotnetapp

RUN apt-get update && apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

ENV GITHUB_REPO_FULL_NAME GyrosWorkshop/Wukong
RUN DOWNLOAD_URL=$(curl -sL https://api.github.com/repos/${GITHUB_REPO_FULL_NAME}/releases | grep browser_download_url | head -n1 | sed 's/"//g' | awk '{ print $2 }' | xargs) && \
    echo "Latest Url: ${DOWNLOAD_URL}" && \
    curl -sL "${DOWNLOAD_URL}" | tar xzv && \
    chmod +x wukong-dist_linux-x64/Wukong

COPY start.sh /
RUN chmod +x /start.sh
COPY appsettings.template.json .

EXPOSE 5000
CMD ["/start.sh"]
