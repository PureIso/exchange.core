import { inject, InjectionToken } from '@angular/core';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { DEFAULT_EXCHANGE_HUB_URL } from './runtime-config';

export interface ExchangeHubConnection {
  on(methodName: string, handler: (...args: any[]) => void): void;
  onclose(handler: (error?: Error) => void): void;
  onreconnected(handler: (connectionId?: string) => void): void;
  onreconnecting(handler: (error?: Error) => void): void;
  start(): Promise<void>;
  stop(): Promise<void>;
  invoke<T = unknown>(methodName: string, ...args: unknown[]): Promise<T>;
}

export const EXCHANGE_HUB_URL = new InjectionToken<string>('EXCHANGE_HUB_URL', {
  providedIn: 'root',
  factory: () => DEFAULT_EXCHANGE_HUB_URL,
});

export const EXCHANGE_HUB_CONNECTION = new InjectionToken<ExchangeHubConnection>(
  'EXCHANGE_HUB_CONNECTION',
  {
    providedIn: 'root',
    factory: () =>
      new HubConnectionBuilder()
        .withUrl(inject(EXCHANGE_HUB_URL))
        .withAutomaticReconnect([0, 2_000, 10_000, 30_000])
        .configureLogging(LogLevel.Warning)
        .build(),
  },
);
