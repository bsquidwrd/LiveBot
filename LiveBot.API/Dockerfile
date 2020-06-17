#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV DiscordToken="Mzc5NDk1NjI1MDg1MTU3Mzc3.Xukusg.FrLj6VaAxEHL7jnkAf2OBEzsmsI"
ENV LiveBotConnectionString="Persist Security Info=True;Password=livebot;Username=livebot;Database=livebot;Host=192.168.254.12;Port=5432;Trust Server Certificate=true"
ENV RabbitMQ_URL="192.168.254.12/livebot-test"
ENV RabbitMQ_Username="guest"
ENV RabbitMQ_Password="guest"
ENV TwitchClientId="umqnym0i4g4n6vo5tsiyx60f0wc4by"
ENV TwitchClientSecret="zba9diay4169wjr6qyl4ov5oaizl13"

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["LiveBot.API/LiveBot.API.csproj", "LiveBot.API/"]
RUN dotnet restore "LiveBot.API/LiveBot.API.csproj"
COPY . .
WORKDIR "/src/LiveBot.API"
RUN dotnet build "LiveBot.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LiveBot.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LiveBot.API.dll"]