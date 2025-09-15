#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# .NET 8 ASP.NET images default to 8080/8443
EXPOSE 8080
EXPOSE 8443
ENV ASPNETCORE_URLS="http://+:8080"
ENV LiveBot_connectionstring=""
ENV LiveBot_IsDebug="True"
ENV LiveBot_datadogapikey=""
ENV RabbitMQ_URL=""
ENV RabbitMQ_Username=""
ENV RabbitMQ_Password=""
ENV LiveBot_TwitchClientId=""
ENV LiveBot_TwitchClientSecret=""
ENV HEALTHCHECK_PATH="/"

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["LiveBot.Watcher.Twitch/LiveBot.Watcher.Twitch.csproj", "LiveBot.Watcher.Twitch/"]
COPY ["LiveBot.Core.HealthCheck/LiveBot.Core.HealthCheck.csproj", "LiveBot.Core.HealthCheck/"]
RUN dotnet restore "LiveBot.Watcher.Twitch/LiveBot.Watcher.Twitch.csproj" \
    && dotnet restore "LiveBot.Core.HealthCheck/LiveBot.Core.HealthCheck.csproj"
COPY . .
WORKDIR "/src/LiveBot.Watcher.Twitch"
RUN dotnet build "LiveBot.Watcher.Twitch.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LiveBot.Watcher.Twitch.csproj" -c Release -o /app/publish
RUN dotnet publish "/src/LiveBot.Core.HealthCheck/LiveBot.Core.HealthCheck.csproj" -c Release -o /app/healthcheck-publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /app/healthcheck-publish /app/healthcheck
COPY --from=publish /app/healthcheck-publish/LiveBot.Core.HealthCheck.* /app/
HEALTHCHECK --interval=30s --timeout=10s --retries=5 CMD ["dotnet", "/app/healthcheck/LiveBot.Core.HealthCheck.dll"]
ENTRYPOINT ["dotnet", "LiveBot.Watcher.Twitch.dll"]
