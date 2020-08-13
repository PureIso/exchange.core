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
    private hubUrlChange: boolean = false;
    private serverHubInitId: any = -1;
    private hubConnection: HubConnection;
    private hubUrl = this.config.setting["HubUrl"];
    private hubName = this.config.setting["HubName"];

    @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;
    @select("exchangeUIContainer") exchangeUIContainer$: Observable<ExchangeUIContainer>;
    exchangeUIContainer: ExchangeUIContainer;

    constructor(private config: AppConfig, private ngRedux: NgRedux<AppState>) {
        super();
        let url: string = this.hubUrl + "/" + this.hubName;
        this.hubConnection = new HubConnectionBuilder().withUrl(url).build();
    }

    start() {
        this.hubConnection.onclose(this.hub_connection_timeout.bind(this));
        this.hubConnection.on("notifyCurrentPrices", super.notifyCurrentPrices);
        this.hubConnection.on("notifyAccountInfo", super.notifyAccountInfo);
        this.hubConnection.on("notifyInformation", super.notifyInformation);
        this.initialiseSubscriptions();
        this.hub_connecting();
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
        console.log("Connecting");
        this.hubConnection
            .start()
            .then(this.hub_connected.bind(this))
            .catch((reason: any) => {
                console.log(reason);
                this.hub_connection_timeout();
            });
    }
    hub_connected() {
        console.log("Connected");
        this.hubUrlChange = false;
        this.timeout = 0;
        clearInterval(this.serverHubInitId);
    }

    hub_connection_timeout() {
        console.log("Timeout");
        clearInterval(this.serverHubInitId);
        this.timeout = this.timeout + 1000;
        this.serverHubInitId = setInterval(this.start.bind(this), this.timeout);
    }
    hub_requestedCurrentPrices() {
        this.hubConnection.invoke("RequestCurrentPrices").catch((err) => console.error(err));
    }
    hub_requestedAccountInfo() {
        this.hubConnection.invoke("RequestAccountInfo").catch((err) => console.error(err));
    }
}
