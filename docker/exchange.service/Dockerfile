FROM mcr.microsoft.com/dotnet/core/sdk:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:5.0-buster AS build
WORKDIR /src
RUN ls
COPY ["exchange.coinbase/exchange.coinbase.csproj", "exchange.coinbase/"]
COPY ["exchange.core/exchange.core.csproj", "exchange.core/"]
COPY ["exchange.service/exchange.service.csproj", "exchange.service/"]
COPY ["exchange.test/exchange.test.csproj", "exchange.test/"]
RUN dotnet restore "exchange.service/exchange.service.csproj"
# RUN dotnet restore "exchange.test/exchange.test.csproj"
COPY . .
WORKDIR "/src/exchange.test"
# RUN dotnet test "exchange.test.csproj" -c Release
WORKDIR "/src/exchange.service"
RUN dotnet publish "exchange.service.csproj" -c Release -o /app/publish

FROM base AS final
COPY --from=build /app/publish ./