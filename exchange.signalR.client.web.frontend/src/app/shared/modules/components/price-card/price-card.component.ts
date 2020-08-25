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

@Component({
    selector: "price-card-component",
    templateUrl: "./price-card.component.html",
})
export class PriceCardComponent implements AfterViewInit, OnInit {
    @Input() priceId: string;
    @Input() applicationName: string;
    @Input() price: Number;
    @ViewChild("priceCard") priceCard: any;
    priceCardNativeElement: HTMLElement;
    @select("exchangeUIContainer") exchangeUIContainer$: Observable<ExchangeUIContainer>;
    exchangeUIContainer: ExchangeUIContainer;

    @select("notificationContainer") notificationContainer$: Observable<
        NotificationContainer
    >;
    notificationContainer: NotificationContainer;

    constructor(private ngRedux: NgRedux<AppState>, private mainService: MainService) {}

    ngOnInit() {
        this.notificationContainer$.subscribe((x: NotificationContainer) => {
            this.notificationContainer = x;
        });
    }

    ngAfterViewInit() {
        this.mainService.hub_requestedCurrentPrices();
    }
}
