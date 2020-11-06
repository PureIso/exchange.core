# exchange.core

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

**[Frontend Solution](exchange.signalR.client.web.frontend/README.md#section)**

- [Angular](https://angular.io/) - (Framework - Frontend)
- [TypeScript](https://www.typescriptlang.org/) - (Language - Frontend)
- [Webpack](https://webpack.js.org/) - (Bundler)
- [npm](https://www.npmjs.com/) - (Package Manager)
- [Karma](http://karma-runner.github.io/0.12/index.html) - (Test Runner)
- [Jasmine](https://jasmine.github.io/) - (Test Framework)
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
- Using visual studio code, open the "exchange.signalR.client.web.frontend" directory and run the following

```shell
npm install
npm start
```

- Navigate to localhost:9000

## Setup - Docker and Docker Compose

- Using visual studio code, download and install docker
- In some cases you might have to clean orphaned images using the following command:

```shell
docker image prune
```

- Using visual studio code, remove volumes with the following command

```shell
docker-compose down -v
```

- Using visual studio code, build the docker image with the following command

```shell
docker-compose up --build
```

## Common Issues

- Byte Order Mark (BOM) casuing issues with UTF-8 file. It can be removed form the init.sh file as part of docker mongo with the following command in bash:

```shell
sed -i $'1s/^\uFEFF//' theFilePath
```

## License

[![License](http://img.shields.io/:license-mit-blue.svg?style=flat-square)](https://choosealicense.com/licenses/mit/)
