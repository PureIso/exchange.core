import { AppState } from "@store/app.state";
import { NgRedux } from "@angular-redux/store";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import * as ExchangeUIContainerActions from "@actions/exchange-ui-container.actions";
import * as NotificationContainerActions from "@actions/exchange-ui-container.actions";
import { Price } from "@interfaces/price.interface";

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

    notifyInformation(applicationName:string, messageType: MessageType, message: string) {
        //console.log(applicationName + " - " + messageType + " - " + message);
    }
}
