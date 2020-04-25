# exchange.core

Cryptocurrency exchange API
**Version: 1.0.0**

System will connect to various exchanges for real-time trade information.\
System will record trading information to be later used for supervised machine learning.\
The project is more so to experiment with various technologies and see how far it goes.

## Programming Paradigms

- Imperitive Programming Paradigm
  - Object Oriented Programming
  - Parallel Processing Approach

## Design

- Dependancy Injection with Adapter Pattern
- Extension Methods

## Development Process

The following development process seems to work well for this project.

- Test Driven Development

## Core Features

- Worker Service with SignalR broadcast
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

## TODO

- XML Comments
- Automatic Trading based on end-user rules
- Saving 15 minutes, 1 hour and 1 day trading candles for neural network
- Full Front End in Windows Form .Net 5
- Alternative Front End with Electron and Angular 9
  - TensorFlowJS
  - RNN Trained Shards
- Binance Trading
- Unit Test
- Trading Indication
  - Relative Strength Index
- Recurrent Neural Network (RNN)
  - Tensorflow
  - Keras
  - Long Short Term Memory (LSTM) model for time series prediction

## Technologies

- [Angular](https://angular.io/) - (Framework - Frontend)
- [TypeScript](https://www.typescriptlang.org/) - (Language - Frontend)
- [Webpack](https://webpack.js.org/) - (Bundler)
- [npm](https://www.npmjs.com/) - (Package Manager)
- [Karma](http://karma-runner.github.io/0.12/index.html) - (Test Runner)
- [Jasmine](https://jasmine.github.io/) - (Test Framework)
- [Rxjs](https://github.com/ReactiveX/rxjs)
- [Sass](http://sass-lang.com/)
- [Bootstrap](http://getbootstrap.com/)
- [Font Awesome](https://fontawesome.com/)
- [.Net 5 C#](https://devblogs.microsoft.com/dotnet/announcing-net-5-0-preview-1/) - (Language - Backend)
- [AspNetCore SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-5.0) - (Real-Time web functionality library)

## Configuration

Open exchange.service/appsettings[Environment].json change the -> "ExchangeSettings" in order to connect to exchange API.

See example for Coinbase:

```json
"ExchangeSettings": {
    "Uri": "wss://ws-feed.gdax.com",
    "APIKey": "API-KEY",
    "PassPhrase": "PASS-PHRASE",
    "Secret": "SECRET",
    "EndpointUrl": "https://api.pro.coinbase.com"
  }
```

## Installation

By default when you build the netcore project.
Start the exchange.service.

## License

[![License](http://img.shields.io/:license-mit-blue.svg?style=flat-square)](http://badges.mit-license.org)
[MIT](https://choosealicense.com/licenses/mit/)
