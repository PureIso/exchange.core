import { TestBed } from '@angular/core/testing';
import { EXCHANGE_HUB_CONNECTION, ExchangeHubConnection } from './exchange-hub-connection';
import { ExchangeHubService } from './exchange-hub.service';
import { AssetInformation } from './asset-information';
import { Product } from './product';
import { Fill } from './fill';
import { FillStatistics } from './fill-statistics';
import { Order } from './order';

class FakeExchangeHubConnection implements ExchangeHubConnection {
  readonly handlers = new Map<string, (...args: any[]) => void>();
  readonly invocations: string[] = [];
  readonly invocationArguments: {
    readonly methodName: string;
    readonly args: readonly unknown[];
  }[] = [];
  startError: Error | null = null;
  started = false;
  stopped = false;

  private closeHandler: (error?: Error) => void = () => undefined;
  private reconnectedHandler: (connectionId?: string) => void = () => undefined;
  private reconnectingHandler: (error?: Error) => void = () => undefined;

  on(methodName: string, handler: (...args: any[]) => void): void {
    this.handlers.set(methodName, handler);
  }

  onclose(handler: (error?: Error) => void): void {
    this.closeHandler = handler;
  }

  onreconnected(handler: (connectionId?: string) => void): void {
    this.reconnectedHandler = handler;
  }

  onreconnecting(handler: (error?: Error) => void): void {
    this.reconnectingHandler = handler;
  }

  async start(): Promise<void> {
    if (this.startError) {
      throw this.startError;
    }
    this.started = true;
  }

  async stop(): Promise<void> {
    this.stopped = true;
  }

  async invoke<T = unknown>(methodName: string, ...args: unknown[]): Promise<T> {
    this.invocations.push(methodName);
    this.invocationArguments.push({ methodName, args });
    return undefined as T;
  }

  emit(methodName: string, ...args: unknown[]): void {
    this.handlers.get(methodName)?.(...args);
  }

  reconnecting(error?: Error): void {
    this.reconnectingHandler(error);
  }

  reconnected(): void {
    this.reconnectedHandler('connection-id');
  }

  close(error?: Error): void {
    this.closeHandler(error);
  }
}

describe('ExchangeHubService', () => {
  let connection: FakeExchangeHubConnection;
  let service: ExchangeHubService;

  beforeEach(() => {
    connection = new FakeExchangeHubConnection();
    TestBed.configureTestingModule({
      providers: [{ provide: EXCHANGE_HUB_CONNECTION, useValue: connection }],
    });
    service = TestBed.inject(ExchangeHubService);
  });

  it('connects and requests the initial read-only hub state', async () => {
    await service.connect();

    expect(connection.started).toBe(true);
    expect(service.connectionState()).toBe('connected');
    expect(connection.invocations).toEqual([
      'RequestedApplications',
      'RequestedProducts',
      'RequestedCurrentPrices',
      'RequestedAccountInfo',
      'RequestedMainCurrency',
    ]);
  });

  it('uses the backend main-currency, asset, and subscription contracts', async () => {
    await service.connect();
    connection.emit('NotifyApplications', ['Coinbase']);
    connection.emit('NotifyMainCurrency', 'Coinbase', 'EUR');
    connection.emit('NotifySubscriptionStatus', 'Coinbase', true);
    const asset: AssetInformation = {
      product_id: '',
      twenty_four_hour_price_change: 125,
      twenty_four_hour_price_percentage_change: 1.5,
      currency_price: 65000,
      order_side: 0,
      best_bid: '64999',
      best_ask: '65001',
      base_currency_symbol: 'BTC',
      quote_currency_symbol: 'EUR',
    };
    connection.emit('NotifyAssetInformation', 'Coinbase', { 'BTC-EUR': asset });

    service.toggleProductSelection('ETH-EUR', true);
    service.toggleProductSelection('BTC-EUR', true);
    await service.requestSelectedProducts();

    expect(service.mainCurrency()).toBe('EUR');
    expect(service.subscriptionRunning()).toBe(true);
    expect(service.assetInformation()[0]?.product_id).toBe('BTC-EUR');
    expect(service.selectedSymbols()).toEqual(['BTC-EUR', 'ETH-EUR']);
    expect(connection.invocationArguments.at(-1)).toEqual({
      methodName: 'RequestedSubscription',
      args: ['Coinbase', ['BTC-EUR', 'ETH-EUR']],
    });
  });

  it('requests and maps read-only order and UUID-safe fill history', async () => {
    await service.connect();
    connection.emit('NotifyApplications', ['Coinbase']);
    connection.emit('NotifyProductChange', 'Coinbase', [
      {
        id: 'BTC-EUR',
        base_currency: 'BTC',
        quote_currency: 'EUR',
        base_min_size: '0.0001',
        base_max_size: '10',
        quote_increment: '0.01',
      },
    ] satisfies Product[]);
    const orders: Order[] = [
      {
        id: 'order-one',
        size: '0.25',
        product_id: 'BTC-EUR',
        side: 'buy',
        stp: 'dc',
        fund: '',
        type: 'limit',
        post_only: true,
        price: '65000',
        time_in_force: 'GTC',
        created_at: '2026-08-15T10:00:00Z',
        fill_fees: '0.10',
        filled_size: '0.25',
        executed_value: '16250',
        status: 'done',
        settled: true,
      },
    ];
    const fills: Fill[] = [
      {
        trade_id: 0,
        external_trade_id: '34b080bf-fcfd-445a-832b-46b5ddc65601',
        product_id: 'BTC-EUR',
        price: '65000',
        size: '0.25',
        order_id: 'order-one',
        side: 'buy',
        fee: '0.10',
        settled: true,
        created_at: '2026-08-15T10:01:00Z',
      },
    ];

    await service.requestHistory();
    connection.emit('NotifyOrders', 'Coinbase', orders);
    connection.emit('NotifyFills', 'Coinbase', fills);

    expect(connection.invocationArguments.slice(-2)).toEqual([
      { methodName: 'RequestedOrder', args: ['Coinbase', 'BTC-EUR'] },
      { methodName: 'RequestedFills', args: ['Coinbase', 'BTC-EUR'] },
    ]);
    expect(service.orders()[0]?.fill_fees).toBe('0.10');
    expect(service.fills()[0]?.external_trade_id).toBe('34b080bf-fcfd-445a-832b-46b5ddc65601');
    expect(service.historyLoading()).toBe(false);
  });

  it('requests and stores fill statistics by exchange and product', async () => {
    await service.connect();
    connection.emit('NotifyApplications', ['Coinbase']);
    connection.emit('NotifyProductChange', 'Coinbase', [
      {
        id: 'BTC-EUR',
        base_currency: 'BTC',
        quote_currency: 'EUR',
        base_min_size: '0.0001',
        base_max_size: '10',
        quote_increment: '0.01',
      },
    ] satisfies Product[]);
    const statistics: FillStatistics = {
      product_id: 'BTC-EUR',
      base_currency: 'BTC',
      quote_currency: 'EUR',
      mini_fill_sell_above_list: [{ price: 65100, size: 0.1 }],
      mini_fill_buy_below_list: [{ price: 64000, size: 0.2 }],
    };

    await service.requestFillStatistics();
    connection.emit('NotifyFillStatistics', 'Coinbase', statistics);

    expect(connection.invocationArguments.at(-1)).toEqual({
      methodName: 'RequestedFillStatistics',
      args: ['Coinbase', 'BTC-EUR'],
    });
    expect(service.fillStatistics()).toEqual(statistics);
    expect(service.statisticsLoading()).toBe(false);
  });

  it('handles unavailable fill statistics without dereferencing null', async () => {
    await service.connect();

    connection.emit('NotifyFillStatistics', 'Coinbase', null);

    expect(service.fillStatistics()).toBeNull();
    expect(service.lastError()).toBe('Fill statistics are not available for the selected product.');
  });

  it('maps and sorts finite price and account values for the selected exchange', async () => {
    await service.connect();
    connection.emit('NotifyApplications', ['Coinbase']);

    connection.emit('NotifyCurrentPrices', 'Coinbase', {
      'ETH-EUR': 3100.5,
      'BTC-EUR': 65000.25,
      invalid: Number.NaN,
    });
    connection.emit('NotifyAccountInfo', 'Coinbase', { ETH: 2.5, EUR: 1250 });

    expect(service.currentPrices()).toEqual([
      { asset: 'BTC-EUR', value: 65000.25 },
      { asset: 'ETH-EUR', value: 3100.5 },
    ]);
    expect(service.accountBalances()).toEqual([
      { asset: 'ETH', value: 2.5 },
      { asset: 'EUR', value: 1250 },
    ]);
  });

  it('maps applications and products without changing the backend contract', async () => {
    await service.connect();
    const products: Product[] = [
      {
        id: 'ETH-EUR',
        base_currency: 'ETH',
        quote_currency: 'EUR',
        base_min_size: '0.001',
        base_max_size: '100',
        quote_increment: '0.01',
      },
      {
        id: 'BTC-EUR',
        base_currency: 'BTC',
        quote_currency: 'EUR',
        base_min_size: '0.0001',
        base_max_size: '10',
        quote_increment: '0.01',
      },
    ];

    connection.emit('NotifyApplications', ['Coinbase', 'Binance', 'Coinbase']);
    connection.emit('NotifyProductChange', 'Binance', products);

    expect(service.applications()).toEqual(['Binance', 'Coinbase']);
    expect(service.selectedApplication()).toBe('Binance');
    expect(service.products().map((product) => product.id)).toEqual(['BTC-EUR', 'ETH-EUR']);
  });

  it('tracks failed starts and automatic reconnect transitions', async () => {
    connection.startError = new Error('backend unavailable');

    await service.connect();

    expect(service.connectionState()).toBe('failed');
    expect(service.lastError()).toBe('backend unavailable');
    expect(connection.invocations).toEqual([]);

    connection.reconnecting(new Error('network changed'));
    expect(service.connectionState()).toBe('reconnecting');
    connection.reconnected();
    await Promise.resolve();
    expect(service.connectionState()).toBe('connected');
  });
});
