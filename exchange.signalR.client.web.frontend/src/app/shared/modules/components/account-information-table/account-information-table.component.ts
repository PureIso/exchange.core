import { Component,Input, OnInit } from "@angular/core";
import { NgRedux, select } from "@angular-redux/store";
import { AppState } from "@store/app.state";
import { Observable } from "rxjs";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { MainService } from "@services/main.service";
import { animate, state, style, transition, trigger } from '@angular/animations';
import { AssetInformation } from "@interfaces/asset-information.interface";
import { AssetInformationContainer } from "@interfaces/asset-information-container.interface";
import { DisplayContainer } from "@interfaces/display-container.interface";
import * as DisplayContainerActions from "@actions/display-container.actions";

@Component({
    selector: "account-information-table-component",
    templateUrl: "./account-information-table.component.html",
    styleUrls: ["./account-information-table.component.css"]
})
export class AccountInformationTableComponent implements OnInit {
    @Input() applicationName: string;
    columnsToDisplay = ['product_id', 'currency_price','twenty_four_hour_price_change','best_bid','best_ask','showTrade', 'showFills' ];

    @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;
    @select("assetInformationContainer") assetInformationContainer$: Observable<AssetInformationContainer>;
    assetInformationContainer: AssetInformationContainer;
    @select("displayContainer") displayContainer$: Observable<DisplayContainer>;
    displayContainer: DisplayContainer;
    accountInformationList: AssetInformation[];

    constructor(private ngRedux: NgRedux<AppState>, private mainService: MainService) {
        this.accountInformationList = new Array();
    }

    ngOnInit(){
        this.notificationContainer$.subscribe((x: NotificationContainer) => {
            this.notificationContainer = x;
        });
        this.displayContainer$.subscribe((x: DisplayContainer) => this.displayContainer = x);
        this.assetInformationContainer$.subscribe((x: AssetInformationContainer) => {
            this.assetInformationContainer = x;
            this.accountInformationList = new Array();
            if(x.assetInformation.length > 0){
                let currentAssetInfo: AssetInformation[] = x.assetInformation.filter((asset:AssetInformation) => {
                    return asset.application_name == this.applicationName;
                });
                if(currentAssetInfo != undefined){
                    this.accountInformationList = currentAssetInfo;
                }
            }  
        });
    }
    ngAfterViewInit() {
        this.mainService.hub_requestedAccountInfo();
    }

    toggleFills(symbol:string) {
        let payload: DisplayContainer = this.displayContainer;
        payload.display.showFillsView = !this.displayContainer.display.showFillsView;
        payload.application_name = this.applicationName;
        if(symbol == null || symbol == undefined){
            payload.display.showFillsView = false;
        }   
        if(payload.display.showFillsView == true){
            this.mainService.hub_requestedFills(this.applicationName,symbol);
        }
        let displayContainerActions: DisplayContainerActions.Actions = new DisplayContainerActions.CRUDDisplayContainer(payload);
        displayContainerActions.updateDisplayState(payload.display,symbol,this.applicationName);
        this.ngRedux.dispatch({ type: displayContainerActions.type, payload: displayContainerActions.payload });
    }
    toggleTrade(symbol:string) {
        let payload: DisplayContainer = this.displayContainer;
        payload.display.showProductTradeView = !this.displayContainer.display.showProductTradeView;
        payload.application_name = this.applicationName;
        if(symbol == null || symbol == undefined){
            payload.display.showProductTradeView = false;
        }
        if(payload.display.showProductTradeView == true){
            this.mainService.hub_requestedOrder(this.applicationName,symbol);
        }
        let displayContainerActions: DisplayContainerActions.Actions = new DisplayContainerActions.CRUDDisplayContainer(payload);
        displayContainerActions.updateDisplayState(payload.display,symbol,this.applicationName);
        this.ngRedux.dispatch({ type: displayContainerActions.type, payload: displayContainerActions.payload });
    }
}