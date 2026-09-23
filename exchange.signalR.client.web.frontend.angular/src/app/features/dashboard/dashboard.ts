import { Component, inject } from '@angular/core';
import { ExchangeHubService } from '../../core/exchange-hub.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  protected readonly hub = inject(ExchangeHubService);

  protected toggleProduct(productId: string, event: Event): void {
    this.hub.toggleProductSelection(productId, (event.target as HTMLInputElement).checked);
  }
}
