import { Component, inject } from '@angular/core';
import { ExchangeHubService } from '../../core/exchange-hub.service';

@Component({
  selector: 'app-history',
  templateUrl: './history.html',
  styleUrl: './history.scss',
})
export class History {
  protected readonly hub = inject(ExchangeHubService);

  protected selectHistoryProduct(event: Event): void {
    this.hub.selectHistoryProduct((event.target as HTMLSelectElement).value);
  }
}
