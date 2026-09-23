# ExchangeWeb

This is the maintained Angular 22 browser frontend for Exchange Core.
The retired Angular/Redux/Electron application was removed after feature, protocol, and Docker cutover acceptance.

## Implemented slice

- Load the hub URL from `runtime-config.json`, defaulting to `http://localhost:5000/hubs/exchange`, and connect with `@microsoft/signalr`.
- Request applications and products using the existing .NET hub method names.
- Request and display current-price and read-only account-summary dictionaries for the selected exchange.
- Request the backend's current main currency with its actual no-argument hub method.
- Select product symbols, apply a read-only market-feed subscription, and display subscription status and asset updates.
- Load read-only order and fill history for an explicitly selected product, preserving external UUID trade identifiers.
- Load null-safe fill statistics separately to avoid duplicating exchange fill requests during normal history refreshes.
- Track connection/reconnect state with Angular signals.
- Exercise the actual SignalR negotiate, JSON handshake/frame, invocation/completion, notification, forced-disconnect, automatic-reconnect, and refresh behavior through a protocol-aware Playwright fixture.
- Select an exchange and display its snake_case product payload.
- Run on port 9000 in development to match the backend's existing CORS configuration.
- Keep shared exchange/connection state in the shell while lazy-loading dashboard/feed and history/statistics routes.
- Load the ML API URL from runtime configuration and lazy-load a prediction-only machine-learning route. It discovers indicator files, dispatches prediction tasks, polls task status, and charts actual versus predicted close prices without exposing model training.

Use Node 24.19.0 and npm 11.17.x. Then run:

```shell
npm ci
npm test -- --watch=false
npx playwright install chromium
npm run test:e2e
npm run build
npm start
```

## Runtime configuration

Development uses `public/runtime-config.json`. A deployment can replace the same file without rebuilding the Angular bundle:

```json
{
  "hubUrl": "https://exchange.example/hubs/exchange",
  "mlApiUrl": "https://ml.example/api/v1/"
}
```

Absolute HTTP(S) URLs and same-origin paths are accepted for both endpoints. The ML base URL is normalized with a trailing slash. Runtime configuration is fetched with `no-store` before Angular starts.

## Production image

The image is available through the non-default `frontend-acceptance` profile in both Compose files. It can also run directly:

```shell
docker build -t exchange-web-angular .
docker run --rm -p 8080:8080 \
  -e EXCHANGE_HUB_URL=https://exchange.example/hubs/exchange \
  -e ML_API_URL=https://ml.example/api/v1/ \
  exchange-web-angular
```

The image uses pinned Node 24.19.0 for `npm ci` and compilation, then stable Nginx 1.30.4 on Alpine 3.24 slim for the runtime. It validates `EXCHANGE_HUB_URL` and `ML_API_URL`, generates `runtime-config.json` in `/tmp`, serves SPA routes, and exposes a health check on port 8080. The runtime runs as UID/GID 101 and supports a read-only root filesystem with only `/tmp` writable.

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.1.2.

## Development server

To start the port-9000 local development server, run:

```bash
npm start
```

Once the server is running, open `http://localhost:9000/`. The application reloads when source files change.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```shell
npm test -- --watch=false
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```shell
npm run test:e2e
```

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
