import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { MainService } from "@services/main.service";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { select } from "@angular-redux/store";
import { Observable } from "rxjs";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";

@Component({
    templateUrl: "./dashboard.component.html",
    styleUrls: ["./dashboard.component.css"],
})

/**
 * DashboardComponent - The dashboard page
 */
export class DashboardComponent implements OnInit {
    @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;
    @select("exchangeUIContainer") exchangeUIContainer$: Observable<ExchangeUIContainer>;
    exchangeUIContainer: ExchangeUIContainer;

    /**
     * DashboardComponent - Constructor call on initialisation
     * @param router - Router to help us navigate to different pages
     */
    constructor(private router: Router, private mainService: MainService) {}
    /**
     * Function called after the constructor and initial ngOnChanges()
     */
    ngOnInit() {
        this.notificationContainer$.subscribe((x: NotificationContainer) => {
            this.notificationContainer = x;
        });
        this.exchangeUIContainer$.subscribe((x: ExchangeUIContainer) => {
            this.exchangeUIContainer = x;
        });
    }
    ngAfterViewInit() {
    }

    onAccountInformationRefresh(){
        this.mainService.hub_requestedAccountInfo();
    }
    onCurrentPricesRefresh(){
        this.mainService.hub_requestedCurrentPrices();
    }
}
