# exchange.core

> Modernization is in progress. The supported local verification path is the isolated .NET 10, Angular 22, Python 3.13, Mongo 8 acceptance stack described below. The maintained development and production Compose definitions use Angular 22 behind the same-origin gateway; the retired Angular/Electron client and its legacy Compose profile have been removed.

> The MongoDB/Graylog/Elasticsearch services in `docker-compose.yml` and `docker-compose.prod.yml` remain a legacy migration source, not an approved production deployment. They include incompatible old search/logging versions and host-specific storage. Do not attach their data volumes to newer images or run the custom Mongo repair initializer against valuable data. The target stack, backup/restore drill, and staged migration are tracked under `DATA-01` in the modernization notes.

## Build status

**Master Branch**:
[![Build Status](https://travis-ci.org/PureIso/exchange.core.svg?branch=master)](https://travis-ci.org/PureIso/exchange.core)

**Development Branch**:
[![Build Status](https://travis-ci.org/PureIso/exchange.core.svg?branch=development)](https://travis-ci.org/PureIso/exchange.core)


**Version: 1.0.0**

Cryptocurrency Exchange API

System will connect to various exchanges for real-time trade information.\
System will record trading information to be later used for supervised machine learning.\
The project is more so to experiment with various technologies and see how far it goes.

![alt text](exhange.assets/exchange.jpg?raw=true)

## Programming Paradigms

- Imperitive Programming Paradigm
  - Object Oriented Programming
  - Parallel Processing Approach

## Design

- Dependancy Injection with Adapter Pattern
- Extension Methods

## Exchange API Documentations

The following links are the exchange API documents

- [Binance](https://binance-docs.github.io/apidocs/spot/en/#change-log) - Binance SPOT API Documentation
- [Coinbase](https://docs.pro.coinbase.com/) - Coinbase API Documentation

## Development Process

The following development process seems to work well for this project.

- Test Driven Development

## Core Features

- Docker implementation
- Saving 15 minutes, 1 hour and 1 day trading candles for neural network
- Worker Service with SignalR broadcast
- Binance Trading
  - Acount Information
  - List Orders
  - Post Orders
  - Product Information
  - Ticker Information
  - Fill Information
  - Product Historic Candles
- Coinbase Pro Trading
  - Acount Information
  - Account History
  - Account Holds
  - List Orders
  - Post Orders
  - Product Information
  - Ticker Information
  - Fill Information
  - Product Order Book
  - Product Historic Candles
  - Subscribe to Real-Time Feeds
- Trading Indication
  - Relative Strength Index
- Recurrent Neural Network (RNN)
  - Tensorflow
  - Keras
  - Long Short Term Memory (LSTM) model for time series prediction

## TODO

- XML Comments
- Automatic Trading based on end-user rules
- Front End with Electron and Angular 9
  - TensorFlowJS
  - RNN Trained Shards
- Unit Test
- Implement prediction API via machine learning Flask APP

## Technologies

**[Frontend Solution](exchange.signalR.client.web.frontend.angular/README.md)**

- [Angular](https://angular.io/) - (Framework - Frontend)
- [TypeScript](https://www.typescriptlang.org/) - (Language - Frontend)
- [Angular CLI](https://angular.dev/tools/cli) - (Build tooling)
- [npm](https://www.npmjs.com/) - (Package Manager)
- [Vitest](https://vitest.dev/) - (Unit test framework)
- [Playwright](https://playwright.dev/) - (Browser test framework)
- [Rxjs](https://github.com/ReactiveX/rxjs) - Reactive library for handling asynchronous data calls, callbacks and event-based programs
- [Sass](http://sass-lang.com/)
- [Angular Material](https://material.angular.io/) - UI component Library

**[Backend Solution](exchange.service/README.md#section)**

- [.Net 5 C#](https://devblogs.microsoft.com/dotnet/announcing-net-5-0-preview-1/) - (Language - Backend)
- [AspNetCore SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-5.0) - (Real-Time web functionality library)

**[Machine Learning Solution](exchange.service.machine.learning/README.md#section)**

- [Python 3](https://www.python.org/) - (Language - Backend/Machine Learning)
- [Keras](https://keras.io/) - (Dead Learning Library)
- [Tensorflow](https://www.tensorflow.org/) - (Artificial Intelligence Library)
- [Flask](https://flask.palletsprojects.com/en/1.1.x/) - (MicroWeb Framework)
- [Celery](http://www.celeryproject.org/) - (Distributed Task Queue)
- [RabbitMQ](https://www.rabbitmq.com/) - (Message Broker)

**Database**

- [MongoDB](https://www.mongodb.com/) - (Database Engine)

**Centralized Logging**

- [GrayLog](https://www.graylog.org/) - (Centralized Log Management)

## Setup - Visual Studios and Visual Studio Code

- Using visual studios build solution.
- Run the "copy_plugins.bat", it will copy the plugins to the exchange.service plugin directory (release and debug)
- Using visual studios run the "exchange.service" project.
- Using Visual Studio Code, open `exchange.signalR.client.web.frontend.angular` and run:

```shell
npm ci
npm start
```

- Navigate to <http://localhost:4200> for the Angular development server, or use the Docker acceptance setup below.

## Setup - Docker and Docker Compose

Docker Desktop must be running. Start the modern local acceptance stack from PowerShell:

```powershell
./scripts/start-modern-stack.ps1 -Build
./scripts/test-modern-stack.ps1
```

The supported browser entry point and diagnostic service ports are:

- Angular 22 gateway: <http://localhost:8088>
- Direct frontend diagnostic port: <http://localhost:9080>
- .NET backend health: <http://localhost:5000/health>
- Machine-learning API: <http://localhost:5005/api/v1/predict/>

Stop the stack while retaining its project-scoped volumes:

```powershell
./scripts/stop-modern-stack.ps1
```

Pass `-RemoveVolumes` only when intentionally discarding acceptance Mongo data, ML artifacts, indicators, and logs. The acceptance definition does not start the obsolete frontend, Graylog, Elasticsearch, or use host-specific bind paths. Its reverse proxy provides the same-origin Angular, backend, SignalR, and ML route contract used by the maintained Compose stack. It is a local integration boundary, not an approved production deployment.

## Observability target acceptance

The isolated authenticated Graylog 7.1.6/Data Node 7.1.6/MongoDB 8.0.26 target uses separate named volumes and ignored local secret files. It does not mount legacy data or join the application acceptance network.

```powershell
./scripts/initialize-observability-secrets.ps1
./scripts/start-observability-stack.ps1
./scripts/test-observability-stack.ps1
```

Open <http://127.0.0.1:19000/> and complete Graylog's required first-start certificate preflight. Retrieve the temporary preflight login from recent Graylog logs; after setup, the normal admin password is stored locally in `.secrets/observability/graylog-root-password.txt`. Configure a GELF input only after Data Node is provisioned.

```powershell
docker compose -f docker-compose.observability.acceptance.yml logs graylog
./scripts/stop-observability-stack.ps1
```

The stop script preserves volumes. `-RemoveVolumes` destroys only this isolated target project's MongoDB, Data Node, and Graylog acceptance state and should be used deliberately.

## Common Issues

- Byte Order Mark (BOM) casuing issues with UTF-8 file. It can be removed form the init.sh file as part of docker mongo with the following command in bash:

```shell
sed -i $'1s/^\uFEFF//' theFilePath
```

## License

[![License](http://img.shields.io/:license-mit-blue.svg?style=flat-square)](https://choosealicense.com/licenses/mit/)
