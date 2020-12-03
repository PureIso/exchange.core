import { Component,Input,ViewChild, OnInit } from "@angular/core";
import { NgRedux, select } from "@angular-redux/store";
import { AppState } from "@store/app.state";
import { Observable } from "rxjs";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { MatAccordion } from '@angular/material/expansion';
import { MainService } from "@services/main.service";
import { AccountInformationContainer } from "@interfaces/account-information-container.interface";
import { AccountInformation } from "@interfaces/account-information.interface";

@Component({
    selector: "account-information-component",
    templateUrl: "./account-information.component.html",
})
export class AccountInformationComponent implements OnInit {
    @Input() applicationName: string;
    @ViewChild(MatAccordion) accordion: MatAccordion;
    @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;
    @select("accountInformationContainer") accountInformationContainer$: Observable<AccountInformationContainer>;
    accountInformationContainer: AccountInformationContainer;
    accountInformationList: AccountInformation[];

    constructor(private ngRedux: NgRedux<AppState>, private mainService: MainService) {
        this.accountInformationList = new Array();
    }

    ngOnInit() {
        this.notificationContainer$.subscribe((x: NotificationContainer) => {
            this.notificationContainer = x;
        });
        this.accountInformationContainer$.subscribe((x: AccountInformationContainer) => {
            this.accountInformationContainer = x;
            this.accountInformationList = this.accountInformationContainer.accountInfo.filter((accountInformation:AccountInformation) => {
                return accountInformation.applicationName == this.applicationName;
            });
        });
        this.mainService.hub_requestedAccountInfo();
    }
}
