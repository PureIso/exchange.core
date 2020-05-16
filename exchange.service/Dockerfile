#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/dotnet
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["exchange.service/exchange.service.csproj", "exchange.service/"]
COPY ["exchange.core/exchange.core.csproj", "exchange.core/"]
COPY ["exchange.test/exchange.test.csproj", "exchange.test/"]
COPY ["exchange.signalR.client.web.backend/exchange.signalR.client.web.backend.csproj", "exchange.signalR.client.web.backend/"]

RUN dotnet restore
COPY . .
WORKDIR "/src/exchange.service"
RUN dotnet build "exchange.service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "exchange.service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "exchange.service.dll"]