#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
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

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["LiveBot.Discord.SlashCommands/LiveBot.Discord.SlashCommands.csproj", "LiveBot.Discord.SlashCommands/"]
RUN dotnet restore "LiveBot.Discord.SlashCommands/LiveBot.Discord.SlashCommands.csproj"
COPY . .
WORKDIR "/src/LiveBot.Discord.SlashCommands"
RUN dotnet build "LiveBot.Discord.SlashCommands.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LiveBot.Discord.SlashCommands.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LiveBot.Discord.SlashCommands.dll"]