#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# .NET 8 ASP.NET images default to 8080/8443
EXPOSE 8080
EXPOSE 8443
ENV ASPNETCORE_URLS="http://+:8080"
ENV LiveBot_connectionstring=""
ENV LiveBot_token=""
ENV LiveBot_publickey=""
ENV LiveBot_testguild=""
ENV LiveBot_TwitchClientId=""
ENV LiveBot_TwitchClientSecret=""
ENV LiveBot_IsDebug=""
ENV LiveBot_datadogapikey=""
ENV RabbitMQ_URL=""
ENV RabbitMQ_Username=""
ENV RabbitMQ_Password=""
ENV LiveBot_BotsForDiscord_API=""
ENV LiveBot_BotsOnDiscord_API=""
ENV LiveBot_DiscordBotList_API=""
ENV LiveBot_DiscordBots_API=""
ENV LiveBot_TopGG_API=""
ENV Redis_connectionstring=""
ENV RunWatchersInDiscord="false"
ENV HEALTHCHECK_PATH="/healthcheck"

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["LiveBot.Discord.SlashCommands/LiveBot.Discord.SlashCommands.csproj", "LiveBot.Discord.SlashCommands/"]
COPY ["LiveBot.Core.HealthCheck/LiveBot.Core.HealthCheck.csproj", "LiveBot.Core.HealthCheck/"]
RUN dotnet restore "LiveBot.Discord.SlashCommands/LiveBot.Discord.SlashCommands.csproj" \
    && dotnet restore "LiveBot.Core.HealthCheck/LiveBot.Core.HealthCheck.csproj"
COPY . .
WORKDIR "/src/LiveBot.Discord.SlashCommands"
RUN dotnet build "LiveBot.Discord.SlashCommands.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LiveBot.Discord.SlashCommands.csproj" -c Release -o /app/publish
# Publish healthcheck alongside app
RUN dotnet publish "/src/LiveBot.Core.HealthCheck/LiveBot.Core.HealthCheck.csproj" -c Release -o /app/healthcheck-publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Include healthcheck app (folder and root-level files for compatibility)
COPY --from=publish /app/healthcheck-publish /app/healthcheck
COPY --from=publish /app/healthcheck-publish/LiveBot.Core.HealthCheck.* /app/
# Built-in container healthcheck
# HEALTHCHECK --interval=30s --timeout=10s --retries=5 CMD ["dotnet", "/app/healthcheck/LiveBot.Core.HealthCheck.dll"]
ENTRYPOINT ["dotnet", "LiveBot.Discord.SlashCommands.dll"]
