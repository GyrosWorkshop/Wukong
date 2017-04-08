FROM microsoft/dotnet:1.1.1-runtime
LABEL maintainer="Senorsen <senorsen.zhang@gmail.com>"
WORKDIR /dotnetapp

RUN mkdir -p /dotnetapp
COPY appsettings.template.json .

COPY start.sh /
RUN chmod +x /start.sh

ENV GITHUB_REPO_FULL_NAME GyrosWorkshop/Wukong
#ENV VERSION 1.0.0
RUN DOWNLOAD_URL=$(wget -qO - https://api.github.com/repos/${GITHUB_REPO_FULL_NAME}/releases | grep browser_download_url | head -n1 | sed 's/"//g' | awk '{ print $2 }' | xargs) && \
    echo "Latest Url: ${DOWNLOAD_URL}" && \
        wget -qO - "${DOWNLOAD_URL}" | tar xzv

EXPOSE 5000
CMD ["/start.sh"]
