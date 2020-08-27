import { AppState } from "@store/app.state";
import { NgRedux } from "@angular-redux/store";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import * as ExchangeUIContainerActions from "@actions/exchange-ui-container.actions";
import * as NotificationContainerActions from "@actions/exchange-ui-container.actions";
import { Price } from "@interfaces/price.interface";
import { AccountInfo } from "@interfaces/account-info.interface";
import { ProductInfo } from "@interfaces/product-info.interface";

export class HubClient {
    static redux: NgRedux<AppState>;
    static exchangeUIContainer: ExchangeUIContainer;
    static notificationContainer: NotificationContainer;
    constructor() {}

    setRedux(redux: NgRedux<AppState>) {
        HubClient.redux = redux;
    }
    setExchangeUIContainer(exchangeUIContainer: ExchangeUIContainer) {
        HubClient.exchangeUIContainer = exchangeUIContainer;
    }

    setNotificationContainer(notificationContainer: NotificationContainer) {
        HubClient.notificationContainer = notificationContainer;
    }

    notifyCurrentPrices(applicationName:string, priceRecords: Record<string, number>) {
        let exchangeUIContainerActions: ExchangeUIContainerActions.Actions = new ExchangeUIContainerActions.CRUDExchangeUIContainer(
            HubClient.exchangeUIContainer
        );
        let prices: Price[] = new Array();
        let keyValuePairs = Object.keys(priceRecords);
        keyValuePairs.forEach((key) => {
            let value = priceRecords[key];
            let price: Price = {applicationName: applicationName, asset: key, price: value };
            prices.push(price);
        });
        exchangeUIContainerActions.updatePrices(prices);
        HubClient.redux.dispatch({
            type: exchangeUIContainerActions.type,
            payload: exchangeUIContainerActions.payload,
        });
    }
    notifyAccountInfo(applicationName:string, accountInfo: Record<string, number>) {
        let exchangeUIContainerActions: ExchangeUIContainerActions.Actions = new ExchangeUIContainerActions.CRUDExchangeUIContainer(
            HubClient.exchangeUIContainer
        );
        let accounts: AccountInfo[] = new Array();
        let keyValuePairs = Object.keys(accountInfo);
        keyValuePairs.forEach((key) => {
            let value = accountInfo[key];
            let account: AccountInfo = {applicationName: applicationName, asset: key, balance: value };
            accounts.push(account);
        });
        exchangeUIContainerActions.updateAccountInfo(accounts);
        HubClient.redux.dispatch({
            type: exchangeUIContainerActions.type,
            payload: exchangeUIContainerActions.payload,
        });
    }
    notifyInformation(applicationName:string, messageType: MessageType, message: string) {
    }
    notifyApplications(applicationNames:string[]){
        let exchangeUIContainerActions: ExchangeUIContainerActions.Actions = new ExchangeUIContainerActions.CRUDExchangeUIContainer(
            HubClient.exchangeUIContainer
        );
        exchangeUIContainerActions.updateApplicationNames(applicationNames);
        HubClient.redux.dispatch({
            type: exchangeUIContainerActions.type,
            payload: exchangeUIContainerActions.payload,
        });
    }
    notifyProductChange(applicationName:string, symbols:string[]){
        let exchangeUIContainerActions: ExchangeUIContainerActions.Actions = new ExchangeUIContainerActions.CRUDExchangeUIContainer(
            HubClient.exchangeUIContainer
        );
        symbols.forEach((asset: string) => {
            let productInfo: ProductInfo = {
                applicationName: applicationName,
                asset: asset
            };
            exchangeUIContainerActions.updateProductInfo(productInfo);
        });
        HubClient.redux.dispatch({
            type: exchangeUIContainerActions.type,
            payload: exchangeUIContainerActions.payload,
        });
    }
}
