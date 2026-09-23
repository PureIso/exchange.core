import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ExchangeHubService } from '../../core/exchange-hub.service';
import { History } from './history';

describe('History', () => {
  const hub = {
    connectionState: signal('connected'),
    historyLoading: signal(false),
    statisticsLoading: signal(false),
    historyProduct: signal<string | null>('BTC-EUR'),
    products: signal([{ id: 'BTC-EUR' }]),
    orders: signal([
      {
        id: 'order-one',
        side: 'buy',
        type: 'limit',
        status: 'done',
        size: '0.25',
        filled_size: '0.25',
        price: '65000',
        fill_fees: '0.10',
        created_at: '2026-08-15T10:00:00Z',
      },
    ]),
    fills: signal([
      {
        trade_id: 0,
        external_trade_id: '34b080bf-fcfd-445a-832b-46b5ddc65601',
        order_id: 'order-one',
        side: 'buy',
        size: '0.25',
        price: '65000',
        fee: '0.10',
        settled: true,
        created_at: '2026-08-15T10:01:00Z',
      },
    ]),
    fillStatistics: signal({
      base_currency: 'BTC',
      quote_currency: 'EUR',
      mini_fill_sell_above_list: [{ price: 65100, size: 0.1 }],
      mini_fill_buy_below_list: [{ price: 64000, size: 0.2 }],
    }),
    selectHistoryProduct: vi.fn(),
    requestHistory: vi.fn(() => Promise.resolve()),
    requestFillStatistics: vi.fn(() => Promise.resolve()),
  };

  beforeEach(async () => {
    hub.selectHistoryProduct.mockClear();
    hub.requestHistory.mockClear();
    hub.requestFillStatistics.mockClear();
    await TestBed.configureTestingModule({
      imports: [History],
      providers: [{ provide: ExchangeHubService, useValue: hub }],
    }).compileComponents();
  });

  it('renders orders, UUID fills, and statistics', () => {
    const fixture = TestBed.createComponent(History);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="order-history"]')?.textContent).toContain(
      'order-one',
    );
    expect(compiled.querySelector('[data-testid="fill-history"]')?.textContent).toContain(
      '34b080bf-fcfd-445a-832b-46b5ddc65601',
    );
    expect(compiled.querySelector('[data-testid="fill-statistics"]')?.textContent).toContain(
      '65100',
    );
  });

  it('passes history and statistics actions to the hub service', () => {
    const fixture = TestBed.createComponent(History);
    fixture.detectChanges();
    const select = fixture.nativeElement.querySelector(
      '[data-testid="history-product"]',
    ) as HTMLSelectElement;
    select.value = 'BTC-EUR';
    select.dispatchEvent(new Event('change'));
    (
      fixture.nativeElement.querySelector('[data-testid="history-button"]') as HTMLButtonElement
    ).click();
    (
      fixture.nativeElement.querySelector('[data-testid="statistics-button"]') as HTMLButtonElement
    ).click();
    expect(hub.selectHistoryProduct).toHaveBeenCalledWith('BTC-EUR');
    expect(hub.requestHistory).toHaveBeenCalledOnce();
    expect(hub.requestFillStatistics).toHaveBeenCalledOnce();
  });
});
