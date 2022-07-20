# DiscordBot

## Docker Image
```shell
sudo docker build --rm -t discord_bot .
```

## 部署方法
```powershell
docker run -p 5200:80 `
-e APP_DiscordToken=THE_TOKEN_FROM_DISCORD_BOT `
-v D:\PATH_TO_HOST_DIR:/app/data `
--name discord-bot-server `
--restart=always `
discord-bot-server
```
