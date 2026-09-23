# syntax=docker/dockerfile:1.7

ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0.400
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0.11

FROM ${SDK_IMAGE} AS build
WORKDIR /src

COPY global.json ./
COPY exchange.core/exchange.core.csproj exchange.core/
COPY exchange.binance/exchange.binance.csproj exchange.binance/
COPY exchange.coinbase/exchange.coinbase.csproj exchange.coinbase/
COPY exchange.coinbase.advancedtrade/exchange.coinbase.advancedtrade.csproj exchange.coinbase.advancedtrade/
COPY exchange.service/exchange.service.csproj exchange.service/
RUN dotnet restore exchange.service/exchange.service.csproj

COPY . .
RUN dotnet publish exchange.service/exchange.service.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        -p:Platform=x64 \
        -p:UseAppHost=false \
    && dotnet build exchange.binance/exchange.binance.csproj \
        --configuration Release \
        --no-restore \
        --output /app/plugin-binance \
        -p:Platform=x64 \
    && dotnet build exchange.coinbase/exchange.coinbase.csproj \
        --configuration Release \
        --no-restore \
        --output /app/plugin-coinbase \
        -p:Platform=x64

FROM ${RUNTIME_IMAGE} AS runtime
ENV ASPNETCORE_ENVIRONMENT=Docker \
    DOTNET_EnableDiagnostics=0

WORKDIR /app
RUN mkdir -p \
        /app/plugin \
        /exchange/service/logs \
        /exchange/service/indicators \
        /exchange/service/config \
    && chown -R ${APP_UID}:${APP_UID} /app /exchange/service

COPY --from=build --chown=${APP_UID}:${APP_UID} /app/publish ./
COPY --from=build --chown=${APP_UID}:${APP_UID} /app/plugin-binance/exchange.binance.dll ./plugin/
COPY --from=build --chown=${APP_UID}:${APP_UID} /app/plugin-binance/binance.config.ini ./plugin/
COPY --from=build --chown=${APP_UID}:${APP_UID} /app/plugin-coinbase/exchange.coinbase.dll ./plugin/
COPY --from=build --chown=${APP_UID}:${APP_UID} /app/plugin-coinbase/coinbase.config.ini ./plugin/

USER ${APP_UID}
EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD ["bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/5000 && printf 'GET /health HTTP/1.0\\r\\nHost: localhost\\r\\n\\r\\n' >&3 && grep -q '200 OK' <&3"]

ENTRYPOINT ["dotnet", "exchange.service.dll"]
