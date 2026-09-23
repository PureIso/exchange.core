import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { EXCHANGE_HUB_URL } from './app/core/exchange-hub-connection';
import { ML_API_URL } from './app/core/ml-prediction.service';
import { loadRuntimeConfig } from './app/core/runtime-config';

loadRuntimeConfig()
  .then((runtimeConfig) =>
    bootstrapApplication(App, {
      ...appConfig,
      providers: [
        ...(appConfig.providers ?? []),
        { provide: EXCHANGE_HUB_URL, useValue: runtimeConfig.hubUrl },
        { provide: ML_API_URL, useValue: runtimeConfig.mlApiUrl },
      ],
    }),
  )
  .catch((error: unknown) => console.error(error));
