import { Component, OnInit, ViewEncapsulation } from "@angular/core";
import { Router } from "@angular/router";
import { MainService } from "@services/main.service";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { NgRedux, select } from "@angular-redux/store";
import { Observable } from "rxjs";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { DisplayContainer } from "@interfaces/display-container.interface";
import { AnalyticsDataContainer } from "@interfaces/analytics-data-container";
import * as AnalyticsDataContainerActions from "@actions/analytics-data-container.actions";
import { AppState } from "@store/app.state";

@Component({
    templateUrl: "./machinelearning.component.html"
})

/**
 * TradeComponent - The Trade page
 */
export class MachineLearningComponent implements OnInit {
    // @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    // notificationContainer: NotificationContainer;
    @select("exchangeUIContainer") exchangeUIContainer$: Observable<ExchangeUIContainer>;
    exchangeUIContainer: ExchangeUIContainer;
    @select("displayContainer") displayContainer$: Observable<DisplayContainer>;
    displayContainer: DisplayContainer;
    @select("analyticsDataContainer") analyticsDataContainer$: Observable<AnalyticsDataContainer>;
    analyticsDataContainer: AnalyticsDataContainer;

    /**
     * DashboardComponent - Constructor call on initialisation
     * @param router - Router to help us navigate to different pages
     */
    constructor(private router: Router, private ngRedux: NgRedux<AppState>, private mainService: MainService) {
    }
    /**
     * Function called after the constructor and initial ngOnChanges()
     */
    ngOnInit() {
        // this.notificationContainer$.subscribe((x: NotificationContainer) => {
        //     this.notificationContainer = x;
        // });
        this.exchangeUIContainer$.subscribe((x: ExchangeUIContainer) => {
            this.exchangeUIContainer = x;
        });
        this.displayContainer$.subscribe((x: DisplayContainer) => {
            this.displayContainer = x;
        });
        this.analyticsDataContainer$.subscribe((x: AnalyticsDataContainer) => {
            this.analyticsDataContainer = x;
        });
    }

    onGetIndicatorFilenames() {
        // this.mainService.get_indicator_filenames()
        //     .subscribe(
        //         (indicators: any) => {
        //             console.log(indicators);
        //             let analyticsDataContainerActions: AnalyticsDataContainerActions.Actions = new AnalyticsDataContainerActions.CRUDAnalyticsDataContainer(
        //                 this.analyticsDataContainer
        //             );
        //             analyticsDataContainerActions.updateIndicatorNameList(indicators);
        //             this.ngRedux.dispatch({
        //                 type: analyticsDataContainerActions.type,
        //                 payload: analyticsDataContainerActions.payload,
        //             });
        //         },
        //         (error: any) => {
        //             //ERROR
        //         });
    }
}
