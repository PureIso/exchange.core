import { Page, WebSocketRoute } from '@playwright/test';

const RECORD_SEPARATOR = '\u001e';

interface HubInvocation {
  readonly target: string;
  readonly arguments: readonly unknown[];
}

interface ClientHubMessage {
  readonly type?: number;
  readonly target?: string;
  readonly arguments?: readonly unknown[];
  readonly invocationId?: string;
  readonly protocol?: string;
}

export class SignalRHubFixture {
  readonly invocations: HubInvocation[] = [];
  private readonly sockets: WebSocketRoute[] = [];
  private pauseHandshake = false;
  private pausedSocket: WebSocketRoute | null = null;
  private negotiationCount = 0;

  get connectionCount(): number {
    return this.sockets.length;
  }

  async register(page: Page): Promise<void> {
    await page.route('**/hubs/exchange/negotiate**', (route) => {
      this.negotiationCount += 1;
      const id = `fixture-connection-${this.negotiationCount}`;
      return route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          negotiateVersion: 1,
          connectionId: id,
          connectionToken: id,
          availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text'] }],
        }),
      });
    });

    await page.routeWebSocket(/\/hubs\/exchange(?:\?|$)/, (socket) => {
      this.sockets.push(socket);
      const connectionNumber = this.sockets.length;
      socket.onMessage((message) => this.handleClientFrames(socket, connectionNumber, message));
    });
  }

  pauseNextHandshake(): void {
    this.pauseHandshake = true;
  }

  resumeHandshake(): void {
    if (!this.pausedSocket) {
      throw new Error('No SignalR handshake is waiting to resume.');
    }
    this.pausedSocket.send(frame({}));
    this.pausedSocket = null;
    this.pauseHandshake = false;
  }

  async disconnectLatest(): Promise<void> {
    const socket = this.sockets.at(-1);
    if (!socket) {
      throw new Error('No SignalR fixture connection is active.');
    }
    await socket.close({ code: 1011, reason: 'Fixture reconnect' });
  }

  hasInvocation(target: string, expectedArguments?: readonly unknown[]): boolean {
    return this.invocations.some(
      (invocation) =>
        invocation.target === target &&
        (expectedArguments === undefined ||
          JSON.stringify(invocation.arguments) === JSON.stringify(expectedArguments)),
    );
  }

  private handleClientFrames(
    socket: WebSocketRoute,
    connectionNumber: number,
    message: string | Buffer,
  ): void {
    for (const rawFrame of message.toString().split(RECORD_SEPARATOR).filter(Boolean)) {
      const payload = JSON.parse(rawFrame) as ClientHubMessage;
      if (payload.protocol === 'json') {
        if (this.pauseHandshake && connectionNumber > 1) {
          this.pausedSocket = socket;
        } else {
          socket.send(frame({}));
        }
        continue;
      }

      if (payload.type !== 1 || !payload.target) {
        continue;
      }

      const invocation: HubInvocation = {
        target: payload.target,
        arguments: payload.arguments ?? [],
      };
      this.invocations.push(invocation);
      if (payload.invocationId !== undefined) {
        socket.send(frame({ type: 3, invocationId: payload.invocationId, result: null }));
      }
      this.publishResponse(socket, connectionNumber, invocation);
    }
  }

  private publishResponse(
    socket: WebSocketRoute,
    connectionNumber: number,
    invocation: HubInvocation,
  ): void {
    const applicationName = String(invocation.arguments[0] ?? 'Coinbase');
    const productId = String(invocation.arguments[1] ?? 'BTC-EUR');

    switch (invocation.target) {
      case 'RequestedApplications':
        notify(socket, 'NotifyApplications', [['Coinbase', 'Binance']]);
        break;
      case 'RequestedProducts':
        notify(socket, 'NotifyProductChange', ['Coinbase', [product('BTC-EUR', 'BTC', 'EUR')]]);
        notify(socket, 'NotifyProductChange', ['Binance', [product('ETH-EUR', 'ETH', 'EUR')]]);
        break;
      case 'RequestedCurrentPrices':
        notify(socket, 'NotifyCurrentPrices', [
          'Coinbase',
          { 'BTC-EUR': connectionNumber === 1 ? 65000.25 : 66000.5 },
        ]);
        notify(socket, 'NotifyCurrentPrices', ['Binance', { 'ETH-EUR': 3200.75 }]);
        break;
      case 'RequestedAccountInfo':
        notify(socket, 'NotifyAccountInfo', ['Coinbase', { BTC: 1.25, EUR: 500 }]);
        notify(socket, 'NotifyAccountInfo', ['Binance', { ETH: 2.5, EUR: 250 }]);
        break;
      case 'RequestedMainCurrency':
        notify(socket, 'NotifyMainCurrency', ['Coinbase', 'EUR']);
        notify(socket, 'NotifyMainCurrency', ['Binance', 'EUR']);
        break;
      case 'RequestedSubscription':
        notify(socket, 'NotifySubscriptionStatus', [applicationName, true]);
        notify(socket, 'NotifyAssetInformation', [
          applicationName,
          {
            'BTC-EUR': {
              product_id: 'BTC-EUR',
              twenty_four_hour_price_change: 125,
              twenty_four_hour_price_percentage_change: 1.5,
              currency_price: 65000.25,
              order_side: 'buy',
              best_bid: '64999',
              best_ask: '65001',
              base_currency_symbol: 'BTC',
              quote_currency_symbol: 'EUR',
            },
          },
        ]);
        break;
      case 'RequestedOrder':
        notify(socket, 'NotifyOrders', [applicationName, [order(productId)]]);
        break;
      case 'RequestedFills':
        notify(socket, 'NotifyFills', [applicationName, [fill(productId)]]);
        break;
      case 'RequestedFillStatistics':
        notify(socket, 'NotifyFillStatistics', [applicationName, statistics(productId)]);
        break;
    }
  }
}

function frame(payload: unknown): string {
  return `${JSON.stringify(payload)}${RECORD_SEPARATOR}`;
}

function notify(socket: WebSocketRoute, target: string, args: readonly unknown[]): void {
  socket.send(frame({ type: 1, target, arguments: args }));
}

function product(id: string, baseCurrency: string, quoteCurrency: string) {
  return {
    id,
    base_currency: baseCurrency,
    quote_currency: quoteCurrency,
    base_min_size: '0.0001',
    base_max_size: '10',
    quote_increment: '0.01',
  };
}

function order(productId: string) {
  return {
    id: 'order-1',
    size: '0.1',
    product_id: productId,
    side: 'buy',
    stp: '',
    fund: '',
    type: 'limit',
    post_only: false,
    price: '64000',
    time_in_force: 'GTC',
    created_at: '2026-08-16T10:00:00Z',
    fill_fees: '1.25',
    filled_size: '0.1',
    executed_value: '6400',
    status: 'done',
    settled: true,
  };
}

function fill(productId: string) {
  return {
    trade_id: 42,
    external_trade_id: '8b8ce449-b211-4ade-9349-f86a928b28c8',
    product_id: productId,
    price: '64000',
    size: '0.1',
    order_id: 'order-1',
    side: 'buy',
    fee: '1.25',
    settled: true,
    created_at: '2026-08-16T10:00:00Z',
  };
}

function statistics(productId: string) {
  return {
    product_id: productId,
    quote_currency: 'EUR',
    base_currency: 'BTC',
    mini_fill_sell_above_list: [{ price: 66000, size: 0.05 }],
    mini_fill_buy_below_list: [{ price: 63000, size: 0.04 }],
  };
}
