#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["exchange.service/exchange.service.csproj", "exchange.service/"]
RUN dotnet restore "exchange.service/exchange.service.csproj"
COPY . .
WORKDIR "/src/exchange.service"
RUN dotnet build "exchange.service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "exchange.service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "exchange.service.dll"]