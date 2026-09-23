import { computed, inject, Injectable, signal } from '@angular/core';
import { EXCHANGE_HUB_CONNECTION, ExchangeHubConnection } from './exchange-hub-connection';
import { AssetValue } from './asset-value';
import { AssetInformation } from './asset-information';
import { Product } from './product';
import { Fill } from './fill';
import { FillStatistics } from './fill-statistics';
import { Order } from './order';

export type ConnectionState =
  'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'failed';

@Injectable({ providedIn: 'root' })
export class ExchangeHubService {
  private readonly connection = inject(EXCHANGE_HUB_CONNECTION);
  private readonly accountBalancesByApplication = signal<
    Readonly<Record<string, readonly AssetValue[]>>
  >({});
  private readonly assetInformationByApplication = signal<
    Readonly<Record<string, readonly AssetInformation[]>>
  >({});
  private readonly currentPricesByApplication = signal<
    Readonly<Record<string, readonly AssetValue[]>>
  >({});
  private readonly fillsByApplication = signal<Readonly<Record<string, readonly Fill[]>>>({});
  private readonly fillStatisticsByApplication = signal<
    Readonly<Record<string, Readonly<Record<string, FillStatistics>>>>
  >({});
  private readonly historyProductByApplication = signal<Readonly<Record<string, string>>>({});
  private readonly ordersByApplication = signal<Readonly<Record<string, readonly Order[]>>>({});
  private readonly productsByApplication = signal<Readonly<Record<string, readonly Product[]>>>({});
  private readonly mainCurrencyByApplication = signal<Readonly<Record<string, string>>>({});
  private readonly selectedSymbolsByApplication = signal<
    Readonly<Record<string, readonly string[]>>
  >({});
  private readonly subscriptionStatusByApplication = signal<Readonly<Record<string, boolean>>>({});

  readonly applications = signal<readonly string[]>([]);
  readonly connectionState = signal<ConnectionState>('disconnected');
  readonly historyLoading = signal(false);
  readonly statisticsLoading = signal(false);
  readonly lastError = signal<string | null>(null);
  readonly selectedApplication = signal<string | null>(null);
  readonly accountBalances = computed(
    () => this.accountBalancesByApplication()[this.selectedApplication() ?? ''] ?? [],
  );
  readonly assetInformation = computed(
    () => this.assetInformationByApplication()[this.selectedApplication() ?? ''] ?? [],
  );
  readonly currentPrices = computed(
    () => this.currentPricesByApplication()[this.selectedApplication() ?? ''] ?? [],
  );
  readonly historyProduct = computed(
    () => this.historyProductByApplication()[this.selectedApplication() ?? ''] ?? null,
  );
  readonly fills = computed(() => {
    const productId = this.historyProduct();
    const fills = this.fillsByApplication()[this.selectedApplication() ?? ''] ?? [];
    return productId ? fills.filter((fill) => fill.product_id === productId) : [];
  });
  readonly fillStatistics = computed(() => {
    const applicationName = this.selectedApplication();
    const productId = this.historyProduct();
    return applicationName && productId
      ? (this.fillStatisticsByApplication()[applicationName]?.[productId] ?? null)
      : null;
  });
  readonly orders = computed(() => {
    const productId = this.historyProduct();
    const orders = this.ordersByApplication()[this.selectedApplication() ?? ''] ?? [];
    return productId ? orders.filter((order) => order.product_id === productId) : [];
  });
  readonly products = computed(
    () => this.productsByApplication()[this.selectedApplication() ?? ''] ?? [],
  );
  readonly mainCurrency = computed(
    () => this.mainCurrencyByApplication()[this.selectedApplication() ?? ''] ?? null,
  );
  readonly selectedSymbols = computed(
    () => this.selectedSymbolsByApplication()[this.selectedApplication() ?? ''] ?? [],
  );
  readonly subscriptionRunning = computed(
    () => this.subscriptionStatusByApplication()[this.selectedApplication() ?? ''] ?? false,
  );

  constructor() {
    this.registerServerEvents(this.connection);
  }

  async connect(): Promise<void> {
    if (this.connectionState() === 'connecting' || this.connectionState() === 'connected') {
      return;
    }

    this.connectionState.set('connecting');
    this.lastError.set(null);

    try {
      await this.connection.start();
      this.connectionState.set('connected');
    } catch (error: unknown) {
      this.connectionState.set('failed');
      this.lastError.set(this.describeError(error));
      return;
    }

    await this.refreshInitialState();
  }

  async disconnect(): Promise<void> {
    await this.connection.stop();
    this.connectionState.set('disconnected');
  }

  selectApplication(applicationName: string): void {
    this.selectedApplication.set(applicationName || null);
  }

  isProductSelected(symbol: string): boolean {
    return this.selectedSymbols().includes(symbol);
  }

  toggleProductSelection(symbol: string, selected: boolean): void {
    const applicationName = this.selectedApplication();
    if (!applicationName) {
      return;
    }

    this.selectedSymbolsByApplication.update((current) => {
      const symbols = new Set(current[applicationName] ?? []);
      if (selected) {
        symbols.add(symbol);
      } else {
        symbols.delete(symbol);
      }
      return {
        ...current,
        [applicationName]: [...symbols].sort((left, right) => left.localeCompare(right)),
      };
    });
  }

  async requestSelectedProducts(): Promise<void> {
    const applicationName = this.selectedApplication();
    const symbols = this.selectedSymbols();
    if (this.connectionState() !== 'connected' || !applicationName || symbols.length === 0) {
      return;
    }

    try {
      await this.connection.invoke('RequestedSubscription', applicationName, [...symbols]);
      this.lastError.set(null);
    } catch (error: unknown) {
      this.lastError.set(this.describeError(error));
    }
  }

  private registerServerEvents(connection: ExchangeHubConnection): void {
    connection.on('NotifyApplications', (applicationNames: string[]) => {
      const names = [...new Set(applicationNames)].sort((left, right) => left.localeCompare(right));
      this.applications.set(names);

      if (!this.selectedApplication() || !names.includes(this.selectedApplication()!)) {
        this.selectedApplication.set(names[0] ?? null);
      }
    });

    connection.on(
      'NotifyProductChange',
      (applicationName: string, products: Product[] | null | undefined) => {
        const normalizedProducts = [...(products ?? [])].sort((left, right) =>
          left.id.localeCompare(right.id),
        );
        this.productsByApplication.update((current) => ({
          ...current,
          [applicationName]: normalizedProducts,
        }));
        this.historyProductByApplication.update((current) => ({
          ...current,
          [applicationName]: current[applicationName] ?? normalizedProducts[0]?.id ?? '',
        }));
      },
    );

    connection.on(
      'NotifyCurrentPrices',
      (applicationName: string, currentPrices: Record<string, number> | null | undefined) => {
        this.currentPricesByApplication.update((current) => ({
          ...current,
          [applicationName]: this.normalizeAssetValues(currentPrices),
        }));
      },
    );

    connection.on(
      'NotifyAccountInfo',
      (applicationName: string, accountBalances: Record<string, number> | null | undefined) => {
        this.accountBalancesByApplication.update((current) => ({
          ...current,
          [applicationName]: this.normalizeAssetValues(accountBalances),
        }));
      },
    );

    connection.on('NotifyMainCurrency', (applicationName: string, currency: string) => {
      this.mainCurrencyByApplication.update((current) => ({
        ...current,
        [applicationName]: currency,
      }));
    });

    connection.on(
      'NotifyAssetInformation',
      (
        applicationName: string,
        assetInformation: Record<string, AssetInformation> | null | undefined,
      ) => {
        const assets = Object.entries(assetInformation ?? {})
          .filter((entry): entry is [string, AssetInformation] => Boolean(entry[1]))
          .map(([productId, asset]) => ({ ...asset, product_id: asset.product_id || productId }))
          .sort((left, right) => left.product_id.localeCompare(right.product_id));
        this.assetInformationByApplication.update((current) => ({
          ...current,
          [applicationName]: assets,
        }));
      },
    );

    connection.on('NotifySubscriptionStatus', (applicationName: string, isRunning: boolean) => {
      this.subscriptionStatusByApplication.update((current) => ({
        ...current,
        [applicationName]: isRunning,
      }));
    });

    connection.on('NotifyOrders', (applicationName: string, orders: Order[] | null | undefined) => {
      this.ordersByApplication.update((current) => ({
        ...current,
        [applicationName]: this.sortNewestFirst(orders ?? []),
      }));
    });

    connection.on('NotifyFills', (applicationName: string, fills: Fill[] | null | undefined) => {
      this.fillsByApplication.update((current) => ({
        ...current,
        [applicationName]: this.sortNewestFirst(fills ?? []),
      }));
    });

    connection.on(
      'NotifyFillStatistics',
      (applicationName: string, statistics: FillStatistics | null | undefined) => {
        if (!statistics?.product_id) {
          this.lastError.set('Fill statistics are not available for the selected product.');
          return;
        }

        const normalized: FillStatistics = {
          ...statistics,
          mini_fill_sell_above_list: statistics.mini_fill_sell_above_list ?? [],
          mini_fill_buy_below_list: statistics.mini_fill_buy_below_list ?? [],
        };
        this.fillStatisticsByApplication.update((current) => ({
          ...current,
          [applicationName]: {
            ...current[applicationName],
            [statistics.product_id]: normalized,
          },
        }));
      },
    );

    connection.onreconnecting((error) => {
      this.connectionState.set('reconnecting');
      this.lastError.set(error ? this.describeError(error) : null);
    });

    connection.onreconnected(() => {
      this.connectionState.set('connected');
      this.lastError.set(null);
      void this.refreshInitialState();
    });

    connection.onclose((error) => {
      this.connectionState.set(error ? 'failed' : 'disconnected');
      this.lastError.set(error ? this.describeError(error) : null);
    });
  }

  private async requestInitialState(): Promise<void> {
    await Promise.all([
      this.connection.invoke('RequestedApplications'),
      this.connection.invoke('RequestedProducts'),
      this.connection.invoke('RequestedCurrentPrices'),
      this.connection.invoke('RequestedAccountInfo'),
      this.connection.invoke('RequestedMainCurrency'),
    ]);
  }

  selectHistoryProduct(productId: string): void {
    const applicationName = this.selectedApplication();
    if (!applicationName) {
      return;
    }
    this.historyProductByApplication.update((current) => ({
      ...current,
      [applicationName]: productId,
    }));
  }

  async requestHistory(): Promise<void> {
    const applicationName = this.selectedApplication();
    const productId = this.historyProduct();
    if (this.connectionState() !== 'connected' || !applicationName || !productId) {
      return;
    }

    this.historyLoading.set(true);
    try {
      await Promise.all([
        this.connection.invoke('RequestedOrder', applicationName, productId),
        this.connection.invoke('RequestedFills', applicationName, productId),
      ]);
      this.lastError.set(null);
    } catch (error: unknown) {
      this.lastError.set(this.describeError(error));
    } finally {
      this.historyLoading.set(false);
    }
  }

  async requestFillStatistics(): Promise<void> {
    const applicationName = this.selectedApplication();
    const productId = this.historyProduct();
    if (this.connectionState() !== 'connected' || !applicationName || !productId) {
      return;
    }

    this.statisticsLoading.set(true);
    try {
      await this.connection.invoke('RequestedFillStatistics', applicationName, productId);
      this.lastError.set(null);
    } catch (error: unknown) {
      this.lastError.set(this.describeError(error));
    } finally {
      this.statisticsLoading.set(false);
    }
  }

  private async refreshInitialState(): Promise<void> {
    try {
      await this.requestInitialState();
    } catch (error: unknown) {
      this.lastError.set(this.describeError(error));
    }
  }

  private describeError(error: unknown): string {
    return error instanceof Error ? error.message : 'The exchange hub connection failed.';
  }

  private normalizeAssetValues(values: Record<string, number> | null | undefined): AssetValue[] {
    return Object.entries(values ?? {})
      .filter((entry): entry is [string, number] => Number.isFinite(entry[1]))
      .map(([asset, value]) => ({ asset, value }))
      .sort((left, right) => left.asset.localeCompare(right.asset));
  }

  private sortNewestFirst<T extends { readonly created_at: string }>(values: readonly T[]): T[] {
    return [...values].sort((left, right) => right.created_at.localeCompare(left.created_at));
  }
}
