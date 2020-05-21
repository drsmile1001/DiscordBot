# DiscordBot

## 部署方法
docker run -p 5200:80 `
>> -e APP_DiscordToken=THE_TOKEN_FROM_DISCORD_BOT `
>> -v D:\PATH_TO_HOST_DIR:/app/app-data `
>> --name discord-bot-server `
>> --restart=always `
>> discord-bot-server