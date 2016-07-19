# Authentication

- Google OAuth `/oauth/google`

# HTTP Endpoint `/api`

## Section `/channel`

### POST `/join/:channelId`

#### Parameter

- previousChannelId

### POST `/updateNextSong/:channelId`

#### Parameter

- siteId
- songId

### POST `/downVote/:channelId`

### POST `/finished/:channelId`

## Section `/song`

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
