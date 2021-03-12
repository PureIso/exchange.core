import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { HubConnection, HubConnectionBuilder } from "@aspnet/signalr";
import { AppConfig } from "@config/config";
import { NgRedux, select } from "@angular-redux/store";
import { AppState } from "@store/app.state";
import { HubClient } from "./hub.client";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { AccountInformationContainer } from "@interfaces/account-information-container.interface";
import { ProductInformationContainer } from "@interfaces/product-information-container.interface";
import { AssetInformationContainer } from "@interfaces/asset-information-container.interface";
import { FillsContainer } from "@interfaces/fills-container.interface";
import { Order } from "@interfaces/order.interface";
import { OrdersContainer } from "@interfaces/orders-container.interface";
import * as NotificationContainerActions from "@actions/notification-container.actions";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { IndicatorInformation } from "@interfaces/indicator-information";
import { PredictionTaskStatus } from "@interfaces/prediction-task-status.interface";
import { PredictionResult } from "@interfaces/prediction-result.interface";

//Interface to the business layer
@Injectable()
export class MainService extends HubClient {
    private timeout: any = 0;
    private connected: boolean;
    private hubUrlChange: boolean = false;
    private serverHubInitId: any = -1;
    private hubConnection: HubConnection;

    private pathAPI = this.config.setting["PathAPI"];
    private headers: HttpHeaders;

    @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;
    @select("exchangeUIContainer") exchangeUIContainer$: Observable<ExchangeUIContainer>;
    exchangeUIContainer: ExchangeUIContainer;
    @select("accountInformationContainer") accountInformationContainer$: Observable<AccountInformationContainer>;
    accountInformationContainer: AccountInformationContainer;
    @select("productInformationContainer") productInformationContainer$: Observable<ProductInformationContainer>;
    productInformationContainer: ProductInformationContainer;
    @select("assetInformationContainer") assetInformationContainer$: Observable<AssetInformationContainer>;
    assetInformationContainer: AssetInformationContainer;
    @select("fillsContainer") fillsContainer$: Observable<FillsContainer>;
    fillsContainer: FillsContainer;
    @select("ordersContainer") ordersContainer$: Observable<OrdersContainer>;
    ordersContainer: OrdersContainer;

    constructor(private config: AppConfig, private ngRedux: NgRedux<AppState>, private httpClient: HttpClient) {
        super();
        this.headers = new HttpHeaders({ "Content-Type": "application/json; charset=utf-8" });
        let url: string = this.config.HUBURL + "/" +  this.config.HUBNAME;
        this.hubConnection = new HubConnectionBuilder().withUrl(url).build();
    }

    startAsync(): Promise<Boolean> {
        //https://www.positronx.io/angular-8-es-6-typescript-promises-examples/
        var promise: Promise<Boolean> = new Promise((resolve, reject) => {
            this.hubConnection.onclose(this.hub_connection_timeout.bind(this));
            this.hubConnection.on("notifyCurrentPrices", super.notifyCurrentPrices);
            this.hubConnection.on("notifyAccountInfo", super.notifyAccountInfo);
            this.hubConnection.on("notifyInformation", super.notifyInformation);
            this.hubConnection.on("notifyApplications", super.notifyApplications);
            this.hubConnection.on("notifyProductChange", super.notifyProductChange);
            this.hubConnection.on("notifyAssetInformation", super.notifyAssetInformation);
            this.hubConnection.on("notifyMainCurrency", super.notifyMainCurrency);
            this.hubConnection.on("notifyFills", super.notifyFills);
            this.hubConnection.on("notifyOrders", super.notifyOrders);
            this.hubConnection.on("notifyFillStatistics", super.notifyFillStatistics);

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
        this.accountInformationContainer$.subscribe((x: AccountInformationContainer) => {
            this.accountInformationContainer = x;
            super.setAccountInformationContainer(x);
        });
        this.productInformationContainer$.subscribe((x: ProductInformationContainer) => {
            this.productInformationContainer = x;
            super.setProductInformationContainer(x);
        });
        this.assetInformationContainer$.subscribe((x: AssetInformationContainer) => {
            this.assetInformationContainer = x;
            super.setAssetInformationContainer(x);
        });
        this.fillsContainer$.subscribe((x: FillsContainer) => {
            this.fillsContainer = x;
            super.setFillsContainer(x);
        });
        this.ordersContainer$.subscribe((x: OrdersContainer) => {
            this.ordersContainer = x;
            super.setOrdersContainer(x);
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
        this.config.HUBURL = hubUrl;
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
        if (!this.connected)
            return;
        this.hubConnection.invoke("RequestedCurrentPrices").catch((err) => console.error(err));
    }
    hub_requestedAccountInfo() {
        if (!this.connected)
            return;
        this.hubConnection.invoke("RequestedAccountInfo").catch((err) => console.error(err));
    }
    hub_requestedProducts() {
        if (!this.connected)
            return;
        this.hubConnection.invoke("RequestedProducts").catch((err) => console.error(err));
    }
    hub_requestedApplications() {
        if (!this.connected)
            return;
        this.hubConnection.invoke("RequestedApplications").catch((err) => console.error(err));
    }
    hub_requestedSubscription(applicationName: string, symbols: string[]) {
        if (!this.connected)
            return;
        this.hubConnection.invoke("RequestedSubscription", applicationName, symbols).catch((err) => console.error(err));
    }
    hub_requestedMainCurrency(applicationName: string, mainCurrency: string) {
        if (!this.connected)
            return;
        this.hubConnection.invoke("RequestedMainCurrency", applicationName, mainCurrency).catch((err) => console.error(err));
    }
    hub_requestedFills(applicationName: string, symbol: string) {
        if (!this.connected)
            return;
        this.hubConnection.invoke("RequestedFills", applicationName, symbol).catch((err) => console.error(err));
    }
    hub_requestedOrder(applicationName: string, symbol: string) {
        if (!this.connected)
            return;
        this.hubConnection.invoke("RequestedOrder", applicationName, symbol).catch((err) => console.error(err));
    }
    hub_requestedCancelAllOrder(applicationName: string, symbol: string) {
        if (!this.connected)
            return;
        this.hubConnection.invoke("RequestedCancelAllOrder", applicationName, symbol).catch((err) => console.error(err));
    }
    hub_requestedCancelOrder(applicationName: string, symbol: string) {
        if (!this.connected)
            return;
        this.hubConnection.invoke("requestedCancelOrder", applicationName, symbol).catch((err) => console.error(err));
    }
    hub_requestedPlaceOrder(applicationName: string, order: Order) {
        if (!this.connected)
            return;
        this.hubConnection.invoke("RequestedPlaceOrder", applicationName, order).catch((err) => console.error(err));
    }
    hub_requestedFillStatistics(applicationName: string, symbol: string) {
        if (!this.connected)
            return;
        this.hubConnection.invoke("RequestedFillStatistics", applicationName, symbol).catch((err) => console.error(err));
    }

    get_indicator_filenames(): Observable<IndicatorInformation> {
        let url = this.pathAPI + 'rnn/';
        return this.httpClient.get<IndicatorInformation>(url, { headers: this.headers });
    }

    post_run_rnn(indicator_file_name: string): Observable<PredictionTaskStatus> {
        let url = this.pathAPI + 'rnn/';
        let data = {'indicator_file': indicator_file_name}
        return this.httpClient.post<PredictionTaskStatus>(
            url,
            data,
            { headers: this.headers });
    }

    post_run_predict(indicator_file_name: string): Observable<PredictionTaskStatus> {
        let url = this.pathAPI + 'predict/';
        let data = {'indicator_file': indicator_file_name}
        return this.httpClient.post<PredictionTaskStatus>(
            url,
            data,
            { headers: this.headers });
    }

    post_task_status(task_id: string): Observable<PredictionResult> {
        let url = this.pathAPI + "taskstatus/";
        let data = {'task_id': task_id}
        return this.httpClient.post<PredictionResult>(
            url,
            data,
            { headers: this.headers });
    }
}