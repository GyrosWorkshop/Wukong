#!/bin/bash

if [ -n "$TRAVIS_TAG" ] && [[ "$TRAVIS_TAG" = v* ]]; then
    DOCKER_TAG="${TRAVIS_TAG#*v}"
else
    if [ -n "$TRAVIS_BRANCH" ]; then
        if [ "$TRAVIS_BRANCH" = "master" ]; then
            DOCKER_TAG=latest
        else
            exit
        fi
    fi
fi
docker login -u="$DOCKER_USERNAME" -p="$DOCKER_PASSWORD"
docker build -t "senorsen/wukong:$DOCKER_TAG" . -f Dockerfile
docker push "senorsen/wukong:$DOCKER_TAG"
