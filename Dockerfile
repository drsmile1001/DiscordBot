FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
WORKDIR /src
COPY ["DiscordBotServer/DiscordBotServer.csproj", "DiscordBotServer/"]
RUN dotnet restore "DiscordBotServer/DiscordBotServer.csproj"
COPY . .
WORKDIR "/src/DiscordBotServer"
RUN dotnet build "DiscordBotServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DiscordBotServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DiscordBotServer.dll"]