# Exchange.SignalR.Client.Web.Frontend

[Angular API](https://angular.io/api)


## Solutions

- [Backend Solution](exchange.service/README.md#section)
- [Machine Learning Solution](exchange.service.machine.learning/README.md#section)

## Prerequisites

Node.js and npm are essential to Angular development.

[Installing Node.js and updating npm](https://docs.npmjs.com/getting-started/installing-node)

## Install npm packages

Install the npm packages described in the `package.json` and verify that it works:

```shell
npm install
npm start
```

### npm scripts

We've captured many of the most useful commands in npm scripts defined in the `package.json`:

* `npm start` - runs the compiler and a server at the same time, both in "watch mode" - localhost:8080.
* `npm run electron:start` - runs the compiler and start application in electron desktop mode.
* `npm run build:dist` - runs the TypeScript compiler once.
* `npm run electron:build:x86` - runs the TypeScript compiler and build electron desktop application to produce an executable file.

Here are the test related scripts:

* `npm test` - compiles, runs karma unit tests
* `npm run test:watch` - compiles, runs and watch karma unit tests
* `npm run e2e` - compiles and run protractor e2e tests, written in Typescript (*e2e-spec.ts)

## Testing

Testing is done using karma/jasmine unit test and protractor end-to-end testing support.

### Unit Tests

TypeScript unit-tests are usually in the `src/app` folder. Their filenames must end in `.spec.ts`.

Look for the example `src/app/app.component.spec.ts`.
Add more `.spec.ts` files as you wish; we configured karma to find them.

Run it with `npm test`

That command first compiles the application, then simultaneously re-compiles and runs the karma test-runner.
Both the compiler and the karma watch for (different) file changes.

Shut it down manually with `Ctrl-C`.

Test-runner output appears in the terminal window.
We can update our app and our tests in real-time, keeping a weather eye on the console for broken tests.
Karma is occasionally confused and it is often necessary to shut down its browser or even shut the command down (`Ctrl-C`) and
restart it. No worries; it's pretty quick.

### End-to-end (E2E) Tests

E2E tests are in the `e2e` directory, side by side with the `src` folder.
Their filenames must end in `.e2e-spec.ts`.

Look for the example `e2e/app.e2e-spec.ts`.
Add more `.e2e-spec.js` files as you wish (although one usually suffices for small projects);
we configured Protractor to find them.

Thereafter, run them with `npm run e2e`.

Shut it down manually with `Ctrl-C`.

## Coding Practices

Passing $event is a dubious practice. [Angular user-input guide](https://angular.io/guide/user-input)
