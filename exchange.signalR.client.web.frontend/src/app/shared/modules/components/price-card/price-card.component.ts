import {Component,Input,AfterViewInit} from "@angular/core";
import { NgRedux, select } from "@angular-redux/store";
import { AppState } from "@store/app.state";
import { Observable } from "rxjs";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { MainService } from "@services/main.service";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { Price } from "@interfaces/price.interface";

@Component({
    selector: "price-card-component",
    templateUrl: "./price-card.component.html",
})
export class PriceCardComponent implements AfterViewInit {
    @Input() applicationName: string;
    @select("exchangeUIContainer") exchangeUIContainer$: Observable<ExchangeUIContainer>;
    exchangeUIContainer: ExchangeUIContainer;
    @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;
    currentPriceList: Price[];

    constructor(private ngRedux: NgRedux<AppState>, private mainService: MainService) {
        this.currentPriceList = new Array();
        this.notificationContainer$.subscribe((x: NotificationContainer) => {
            this.notificationContainer = x;
        });
        this.exchangeUIContainer$.subscribe((x: ExchangeUIContainer) => {
            this.exchangeUIContainer = x;
            //requires ES6 support, Babel or TypeScript
            this.currentPriceList = x.prices.filter((price:Price) => {
                return price.applicationName == this.applicationName;
            });
        });
    }

    ngAfterViewInit() {
        this.mainService.hub_requestedCurrentPrices();
    }
}
