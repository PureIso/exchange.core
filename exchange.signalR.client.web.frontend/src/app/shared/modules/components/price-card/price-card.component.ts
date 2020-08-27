import {
    Component,
    OnInit,
    Input,
    ViewChild,
    AfterViewInit,
} from "@angular/core";
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
export class PriceCardComponent implements AfterViewInit, OnInit {
    @Input() applicationName: string;
    @select("exchangeUIContainer") exchangeUIContainer$: Observable<ExchangeUIContainer>;
    exchangeUIContainer: ExchangeUIContainer;
    @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;
    currentPriceList: Price[];

    constructor(private ngRedux: NgRedux<AppState>, private mainService: MainService) {
        this.currentPriceList = new Array();
    }

    ngOnInit() {
        this.notificationContainer$.subscribe((x: NotificationContainer) => {
            this.notificationContainer = x;
        });
        this.exchangeUIContainer$.subscribe((x: ExchangeUIContainer) => {
            this.exchangeUIContainer = x;
            this.currentPriceList = new Array();
            
            x.prices.forEach((price: Price)=>{
                if(price.applicationName == this.applicationName){
                    this.currentPriceList.push(price);
                }
            });
        });
    }

    ngAfterViewInit() {
        this.mainService.hub_requestedCurrentPrices();
    }
}
