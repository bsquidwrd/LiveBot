## Live Bot
[![GitHub license](https://img.shields.io/github/license/bsquidwrd/LiveBot.svg)](https://github.com/bsquidwrd/LiveBot/blob/master/LICENSE) [![Build status](https://ci.appveyor.com/api/projects/status/mo984v4k8j5k6ema?svg=true)](https://ci.appveyor.com/project/Bsquidwrd47752/livebot) [![Discord](https://discordapp.com/api/guilds/350337137079746581/widget.png?style=shield)](https://discord.gg/zXkb4JP)


## Running
[Click here to have the bot added to your server](https://discordapp.com/oauth2/authorize?client_id=334870738257444865&scope=bot&permissions=518208)

### Example notification
<img src="https://i.imgur.com/n2RXb1E.png" />

## Start Monitoring a Stream
* Run `@Live Bot#5263 monitor perms` to have the Bot run a check for permissions. If you don't get a response, it doesn't even have `Send Messages` permission in that channel
* Start setting up a monitor `@Live Bot#5263 monitor add https://twitch.tv/bsquidwrd`
* Which channel you wish to send notifications in. Mention the channel. Ex: `#live`
* Which message should sent for notifications? Typing `default` will result in:
  * `{role} {name} is live and is playing {game}! {url}` See parameters below for more information
* Do you want to mention a role? Type the name of the role or `none` if you don't want to mention a role. Ex: `everyone`
* That's it!

## Stop Monitoring a Stream
* Run `@Live Bot#5263 monitor stop https://twitch.tv/bsquidwrd`
* That's it!

## List Streams that are being monitored
* `@Live Bot#5263 monitor list`

## Check if the Bot sees a Stream as Live
* `@Live Bot#5263 monitor check https://twitch.tv/bsquidwrd`

## Get a link to my Support Server
* `@Live Bot#5263 support`

## Message Parameters
The below parameters will be replaced at the time of Notification
* `{role}` - Role to ping (if applicable)
* `{name}` - Streamers Name
* `{game}` - Game they are playing
* `{url}` - URL to the stream
* `{title}` - Stream Title


## Thanks
- [ThatOhio](https://github.com/thatohio) and his patience with me while I learned C# and wrote version 2.0 of this bot at the same time
