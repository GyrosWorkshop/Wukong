# Wukong

Wukong is a web-based music sharing platform which is implemented in C# and JavaScript. The application allows users to share and listen their favorite songs together.

[![Build Status](https://travis-ci.org/GyrosWorkshop/Wukong.svg?branch=master)](https://travis-ci.org/GyrosWorkshop/Wukong)
[![](https://images.microbadger.com/badges/image/gyrosworkshop/wukong.svg)](https://microbadger.com/images/gyrosworkshop/wukong "Get your own image badge on microbadger.com")
[![Code Climate](https://codeclimate.com/github/GyrosWorkshop/Wukong.png)](https://codeclimate.com/github/GyrosWorkshop/Wukong)
[![license](https://img.shields.io/github/license/GyrosWorkshop/Wukong.svg)](https://github.com/GyrosWorkshop/Wukong/blob/master/LICENSE)
[![GitHub release](https://img.shields.io/github/release/GyrosWorkshop/Wukong.svg)](https://github.com/GyrosWorkshop/Wukong/releases)

# Authentication

- We currently only support Google OAuth. More will be added soon.
- Google OAuth `/oauth/google`

# HTTP Endpoint `/api` 

Using APIs under `/api` endpoint requires authentication.

## Section `/channel`

The APIs under `channel` section controls the main logic of Wukong service.

### POST `/join/:channelId`

Join a channel using channel ID. Joining a new channel will leave the previous channel automatically.

### POST `/updateNextSong/:channelId`

#### Parameter

- siteId
- songId

### POST `/downVote/:channelId`

If half of the listeners have downvoted, the current song will be stopped and play next song immediately.

### POST `/finished/:channelId`

The frontend should report when the current song finishes.

## Section `/song`

The APIs under `/song` sections serves as a middleware between Wukong backend and Wukong providers so that provider can be added effortlessly without modifying any backend logic.

### POST `/search`

#### Parameter

- key

#### Response

- list<song>
    - artist
    - album
    - artwork
    - title
    - siteId
    - songId

## Section `/user`

###  GET `/userinfo`

#### Response

- userName
- id
- avatar

### GET `/songList/:id`

#### Response

- name
- list<song>
    - artist
    - album
    - artwork
    - title
    - siteId
    - songId
    - length

### POST `/songList[/:id]`

#### Parameter

- name
- list<song>
    - songId
    - siteId

#### Response

- [id]

# WebSocket Endpoint `/`

WebSocket endpoint is used to create an interactive communication between clients and backends.

## Server to Client

### Play

- eventName - "Play"
- song
    - artist
    - album
    - artwork
    - title
    - siteId
    - songId
    - length - double
    - file
- elapsed - double
- user

A client should always stop the already playing track and start playing another when 'Play' event is received.

### UserListUpdated

- eventName - "UserListUpdated"
- users - list
    - userName
    - id
    - avarta

### NextSongUpdated

- eventName = "NextSongUpdated"
- song
    - artist
    - album
    - artwork
    - title
    - siteId
    - songId
    - length - double
    - file

# Notes

* HTTP status code 401 will be sent in response to unauthenticated requests.
* Clients should connect the WebSocket endpoint immediately after joining a channel to receive messages from it. Clients that being disconnected for a period of time will be removed from the channel.
