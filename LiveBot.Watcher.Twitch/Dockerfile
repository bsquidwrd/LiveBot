#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV LiveBot_connectionstring=""
ENV RabbitMQ_URL=""
ENV RabbitMQ_Username=""
ENV RabbitMQ_Password=""
ENV LiveBot_TwitchClientId=""
ENV LiveBot_TwitchClientSecret=""

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["LiveBot.Watcher.Twitch/LiveBot.Watcher.Twitch.csproj", "LiveBot.Watcher.Twitch/"]
RUN dotnet restore "LiveBot.Watcher.Twitch/LiveBot.Watcher.Twitch.csproj"
COPY . .
WORKDIR "/src/LiveBot.Watcher.Twitch"
RUN dotnet build "LiveBot.Watcher.Twitch.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LiveBot.Watcher.Twitch.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LiveBot.Watcher.Twitch.dll"]