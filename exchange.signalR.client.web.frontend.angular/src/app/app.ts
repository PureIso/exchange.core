import { Component, inject, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ExchangeHubService } from './core/exchange-hub.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  protected readonly hub = inject(ExchangeHubService);

  ngOnInit(): void {
    void this.hub.connect();
  }

  protected selectApplication(event: Event): void {
    this.hub.selectApplication((event.target as HTMLSelectElement).value);
  }
}
