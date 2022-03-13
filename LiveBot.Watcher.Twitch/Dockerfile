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
COPY ["LiveBot.Water.Twitch/LiveBot.Water.Twitch.csproj", "LiveBot.Water.Twitch/"]
RUN dotnet restore "LiveBot.Water.Twitch/LiveBot.Water.Twitch.csproj"
COPY . .
WORKDIR "/src/LiveBot.Water.Twitch"
RUN dotnet build "LiveBot.Water.Twitch.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LiveBot.Water.Twitch.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LiveBot.Water.Twitch.dll"]