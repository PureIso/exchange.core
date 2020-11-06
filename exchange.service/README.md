# Exchange Service


## Solutions

- [Frontend Solution](exchange.signalR.client.web.frontend/README.md#section)
- [Machine Learning Solution](exchange.service.machine.learning/README.md#section)

## Configuration

Open exchange.service/appsettings[Environment].json change the -> "ExchangeSettings" in order to connect to exchange API.

Service setting to determine what API endpoints to use

```json
"ExchangeSettings": {
    "TestMode": true
  }
```

```ini
test_uri=wss://ws-feed-public.sandbox.pro.coinbase.com
test_key=
test_passphrase=
test_secret=
test_endpoint=https://api-public.sandbox.pro.coinbase.com
live_uri=wss://ws-feed.pro.coinbase.com
live_key=
live_passphrase=
live_secret=
live_endpoint=https://api.pro.coinbase.com
```