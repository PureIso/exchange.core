import { Component,Input,AfterViewInit } from "@angular/core";
import { NgRedux, select } from "@angular-redux/store";
import { AppState } from "@store/app.state";
import { Observable } from "rxjs";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { MainService } from "@services/main.service";
import {animate, state, style, transition, trigger} from '@angular/animations';
import { AssetInformation } from "@interfaces/asset-information.interface";
import { AssetInformationContainer } from "@interfaces/asset-information-container.interface";

@Component({
    selector: "account-information-table-component",
    templateUrl: "./account-information-table.component.html",
    styleUrls: ["./account-information-table.component.css"],
    animations: [
        trigger('detailExpand', [
          state('collapsed', style({height: '0px', minHeight: '0'})),
          state('expanded', style({height: '*'})),
          transition('expanded <=> collapsed', animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')),
        ]),
      ],
})
export class AccountInformationTableComponent implements AfterViewInit {
    @Input() applicationName: string;
    columnsToDisplay = ['id', 'currency_price','twenty_four_hour_price_change','best_bid','best_ask' ];
    expandedElement: AssetInformation;

    @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;
    @select("assetInformationContainer") assetInformationContainer$: Observable<AssetInformationContainer>;
    assetInformationContainer: AssetInformationContainer;
    accountInformationList: AssetInformation[];

    constructor(private ngRedux: NgRedux<AppState>, private mainService: MainService) {
        this.accountInformationList = new Array();
        this.notificationContainer$.subscribe((x: NotificationContainer) => {
            this.notificationContainer = x;
        });
        this.assetInformationContainer$.subscribe((x: AssetInformationContainer) => {
            this.assetInformationContainer = x;
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
}