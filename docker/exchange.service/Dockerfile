FROM mcr.microsoft.com/dotnet/sdk:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["exchange.coinbase/exchange.coinbase.csproj", "exchange.coinbase/"]
COPY ["exchange.binance/exchange.binance.csproj", "exchange.binance/"]
COPY ["exchange.core/exchange.core.csproj", "exchange.core/"]
COPY ["exchange.service/exchange.service.csproj", "exchange.service/"]
COPY ["exchange.test/exchange.test.csproj", "exchange.test/"]
COPY . .
RUN dotnet restore "exchange.service/exchange.service.csproj"
RUN dotnet restore "exchange.binance/exchange.binance.csproj"
RUN dotnet restore "exchange.coinbase/exchange.coinbase.csproj"
RUN dotnet build "exchange.coinbase/exchange.coinbase.csproj" -c Release -o /app/plugin
RUN dotnet build "exchange.binance/exchange.binance.csproj" -c Release -o /app/plugin
WORKDIR "/app/plugin"
RUN ls
WORKDIR "/src/exchange.service"
RUN dotnet publish "exchange.service.csproj" -c Release -o /app/publish

FROM base AS final
COPY --from=build /app/publish ./
COPY --from=build /app/plugin/exchange.binance.dll ./plugin/
COPY --from=build /app/plugin/exchange.coinbase.dll ./plugin/
COPY --from=build /app/plugin/coinbase.config.ini ./plugin/
COPY --from=build /app/plugin/binance.config.ini ./plugin/