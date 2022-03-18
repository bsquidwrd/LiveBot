#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV LiveBot_token=""
ENV LiveBot_connectionstring=""
ENV RabbitMQ_URL=""
ENV RabbitMQ_Username=""
ENV RabbitMQ_Password=""
ENV LiveBot_TopGG_API=""

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["LiveBot.Discord.Socket/LiveBot.Discord.Socket.csproj", "LiveBot.Discord.Socket/"]
RUN dotnet restore "LiveBot.Discord.Socket/LiveBot.Discord.Socket.csproj"
COPY . .
WORKDIR "/src/LiveBot.Discord.Socket"
RUN dotnet build "LiveBot.Discord.Socket.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LiveBot.Discord.Socket.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=15s --timeout=3s CMD ["dotnet", "LiveBot.Core.HealthCheck.dll", " || exit 1"]
ENTRYPOINT ["dotnet", "LiveBot.Discord.Socket.dll"]