import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { ExchangeHubService } from './core/exchange-hub.service';

describe('App shell', () => {
  const hub = {
    applications: signal<readonly string[]>(['Binance', 'Coinbase']),
    connectionState: signal('connected'),
    lastError: signal<string | null>(null),
    selectedApplication: signal<string | null>('Coinbase'),
    connect: vi.fn(() => Promise.resolve()),
    selectApplication: vi.fn(),
  };

  beforeEach(async () => {
    hub.connect.mockClear();
    hub.selectApplication.mockClear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), { provide: ExchangeHubService, useValue: hub }],
    }).compileComponents();
  });

  it('connects and renders shared application controls', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(hub.connect).toHaveBeenCalledOnce();
    expect(compiled.querySelector('.status')?.textContent?.trim()).toBe('connected');
    expect(compiled.querySelectorAll('nav a')).toHaveLength(3);
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
  });

  it('passes exchange selection to the shared hub state', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const select = fixture.nativeElement.querySelector('#exchange') as HTMLSelectElement;
    select.value = 'Binance';
    select.dispatchEvent(new Event('change'));

    expect(hub.selectApplication).toHaveBeenCalledWith('Binance');
  });
});
