import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ExchangeHubService } from '../../core/exchange-hub.service';
import { Dashboard } from './dashboard';

describe('Dashboard', () => {
  const product = {
    id: 'BTC-EUR',
    base_currency: 'BTC',
    quote_currency: 'EUR',
    base_min_size: '0.0001',
    base_max_size: '10',
    quote_increment: '0.01',
  };
  const hub = {
    connectionState: signal('connected'),
    selectedApplication: signal<string | null>('Coinbase'),
    mainCurrency: signal<string | null>('EUR'),
    selectedSymbols: signal<readonly string[]>(['BTC-EUR']),
    subscriptionRunning: signal(true),
    currentPrices: signal<readonly { asset: string; value: number }[]>([
      { asset: 'BTC-EUR', value: 65000.25 },
    ]),
    accountBalances: signal<readonly { asset: string; value: number }[]>([
      { asset: 'BTC', value: 1.25 },
    ]),
    products: signal<readonly (typeof product)[]>([product]),
    assetInformation: signal([
      {
        product_id: 'BTC-EUR',
        currency_price: 65000.25,
        twenty_four_hour_price_change: 125,
        twenty_four_hour_price_percentage_change: 1.5,
        best_bid: '64999',
        best_ask: '65001',
      },
    ]),
    isProductSelected: vi.fn(() => true),
    toggleProductSelection: vi.fn(),
    requestSelectedProducts: vi.fn(() => Promise.resolve()),
  };

  beforeEach(async () => {
    hub.toggleProductSelection.mockClear();
    hub.requestSelectedProducts.mockClear();
    await TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [{ provide: ExchangeHubService, useValue: hub }],
    }).compileComponents();
  });

  it('renders live prices, balances, products, and asset updates', () => {
    const fixture = TestBed.createComponent(Dashboard);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="current-prices"]')?.textContent).toContain(
      '65000.25',
    );
    expect(compiled.querySelector('[data-testid="account-balances"]')?.textContent).toContain(
      '1.25',
    );
    expect(compiled.querySelector('[data-testid="asset-information"]')?.textContent).toContain(
      '64999',
    );
  });

  it('passes feed selection actions to the hub service', () => {
    const fixture = TestBed.createComponent(Dashboard);
    fixture.detectChanges();
    const checkbox = fixture.nativeElement.querySelector(
      'input[type="checkbox"]',
    ) as HTMLInputElement;
    checkbox.checked = false;
    checkbox.dispatchEvent(new Event('change'));
    (
      fixture.nativeElement.querySelector('[data-testid="subscribe-button"]') as HTMLButtonElement
    ).click();
    expect(hub.toggleProductSelection).toHaveBeenCalledWith('BTC-EUR', false);
    expect(hub.requestSelectedProducts).toHaveBeenCalledOnce();
  });
});
