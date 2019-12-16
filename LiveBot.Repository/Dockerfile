#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["LiveBot.Repository/LiveBot.Repository.csproj", "LiveBot.Repository/"]
RUN dotnet restore "LiveBot.Repository/LiveBot.Repository.csproj"
COPY . .
WORKDIR "/src/LiveBot.Repository"
RUN dotnet build "LiveBot.Repository.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LiveBot.Repository.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LiveBot.Repository.dll"]