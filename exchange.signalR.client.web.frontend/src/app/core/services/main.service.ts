import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { HubConnection, HubConnectionBuilder } from "@aspnet/signalr";
import { AppConfig } from "@config/config";
import { NgRedux, select } from "@angular-redux/store";
import { AppState } from "@store/app.state";
import { HubClient } from "./hub.client";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import * as NotificationContainerActions from "@actions/notification-container.actions";

//Interface to the business layer
@Injectable()
export class MainService extends HubClient {
    private timeout: any = 0;
    private connected: boolean;
    private hubUrlChange: boolean = false;
    private serverHubInitId: any = -1;
    private hubConnection: HubConnection;
    private hubUrl:string = this.config.HUBURL;
    private hubName:string = this.config.HUBNAME;

    @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;
    @select("exchangeUIContainer") exchangeUIContainer$: Observable<ExchangeUIContainer>;
    exchangeUIContainer: ExchangeUIContainer;

    constructor(private config: AppConfig, private ngRedux: NgRedux<AppState>) {
        super();
        let url: string = this.hubUrl + "/" + this.hubName;
        this.hubConnection = new HubConnectionBuilder().withUrl(url).build();
    }

    startAsync():Promise<Boolean> {
        //https://www.positronx.io/angular-8-es-6-typescript-promises-examples/
        var promise:Promise<Boolean> = new Promise((resolve, reject) => {
            this.hubConnection.onclose(this.hub_connection_timeout.bind(this));
            this.hubConnection.on("notifyCurrentPrices", super.notifyCurrentPrices);
            this.hubConnection.on("notifyAccountInfo", super.notifyAccountInfo);
            this.hubConnection.on("notifyInformation", super.notifyInformation);
            this.hubConnection.on("notifyApplications", super.notifyApplications);
            this.hubConnection.on("notifyProductChange", super.notifyProductChange);
            this.initialiseSubscriptions();
            this.hub_connecting();
            return true;
        });
        return promise;      
    }

    initialiseSubscriptions() {
        super.setRedux(this.ngRedux);
        this.notificationContainer$.subscribe((x: NotificationContainer) => {
            this.notificationContainer = x;
            super.setNotificationContainer(x);
        });
        this.exchangeUIContainer$.subscribe((x: ExchangeUIContainer) => {
            this.exchangeUIContainer = x;
            super.setExchangeUIContainer(x);
        });
    }

    exception_handler(title: string, description: string, style: string) {
        let notificationContainerActions: NotificationContainerActions.Actions = new NotificationContainerActions.CRUDNotificationContainer(this.notificationContainer);
        // let notification: Notification;
        // notification.title = title;
        // notificationContainerActions.addNotification(new Notification(){})
    }

    set_Huburl(hubUrl: string) {
        this.hubUrlChange = true;
        this.hubUrl = hubUrl;
        clearInterval(this.serverHubInitId);
    }

    hub_connecting() {
        this.connected = false;
        this.hubConnection
            .start()
            .then(this.hub_connected.bind(this))
            .catch((reason: any) => {
                this.hub_connection_timeout();
            });
    }
    hub_connected() {
        this.connected = true;
        this.hubUrlChange = false;
        this.timeout = 0;
        clearInterval(this.serverHubInitId);
        this.hub_requestedApplications();
        this.hub_requestedProducts();
        this.hub_requestedCurrentPrices();
    }

    hub_connection_timeout() {
        this.connected = false;
        clearInterval(this.serverHubInitId);
        this.timeout = this.timeout + 1000;
        this.serverHubInitId = setInterval(this.startAsync.bind(this), this.timeout);
    }

    hub_requestedCurrentPrices() {
        if(!this.connected)
            return;
        this.hubConnection.invoke("RequestedCurrentPrices").catch((err) => console.error(err));
    }
    hub_requestedAccountInfo() {
        if(!this.connected)
            return;
        this.hubConnection.invoke("RequestedAccountInfo").catch((err) => console.error(err));
    }
    hub_requestedProducts() {
        if(!this.connected)
            return;
        this.hubConnection.invoke("RequestedProducts").catch((err) => console.error(err));
    }
    hub_requestedApplications() {
        if(!this.connected)
            return;
        this.hubConnection.invoke("RequestedApplications").catch((err) => console.error(err));
    }
    hub_requestedSubscription(applicationName:string, symbols: string[]) {
        if(!this.connected)
            return;
        this.hubConnection.invoke("RequestedSubscription",applicationName, symbols).catch((err) => console.error(err));
    }
}