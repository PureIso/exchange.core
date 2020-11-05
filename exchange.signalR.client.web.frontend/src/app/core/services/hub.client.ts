import { AppState } from "@store/app.state";
import { NgRedux } from "@angular-redux/store";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { ProductInformationContainer } from "@interfaces/product-information-container.interface";
import { AccountInformationContainer } from "@interfaces/account-information-container.interface";
import { Price } from "@interfaces/price.interface";
import { AccountInformation } from "@interfaces/account-information.interface";
import { ProductInformation } from "@interfaces/product-information.interface";
import { AssetInformation } from "@interfaces/asset-information.interface";
import { MainCurrency } from "@interfaces/main-currency.interface";
import { AssetInformationContainer } from "@interfaces/asset-information-container.interface";
import * as ExchangeUIContainerActions from "@actions/exchange-ui-container.actions";
import * as NotificationContainerActions from "@actions/exchange-ui-container.actions";
import * as AccountInformationContainerActions from "@actions/account-information-container.actions";
import * as ProductInformationContainerActions from "@actions/product-information-container.actions";
import * as AssetInformationContainerActions from "@actions/asset-information-container.actions";

export class HubClient {
    static redux: NgRedux<AppState>;
    static exchangeUIContainer: ExchangeUIContainer;
    static notificationContainer: NotificationContainer;
    static accountInformationContainer: AccountInformationContainer;
    static productInformationContainer: ProductInformationContainer;
    static assetInformationContainer: AssetInformationContainer;
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
    setAccountInformationContainer(accountInformationContainer: AccountInformationContainer) {
        HubClient.accountInformationContainer = accountInformationContainer;
    }
    setProductInformationContainer(productInformationContainer: ProductInformationContainer) {
        HubClient.productInformationContainer = productInformationContainer;
    }
    setAssetInformationContainer(assetInformationContainer: AssetInformationContainer) {
        HubClient.assetInformationContainer = assetInformationContainer;
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
        let accountInformationContainerActions: AccountInformationContainerActions.Actions = new AccountInformationContainerActions.CRUDAccountInformationContainer(
            HubClient.accountInformationContainer
        );
        let accountInformationList: AccountInformation[] = new Array();
        let keyValuePairs = Object.keys(accountInfo);
        keyValuePairs.forEach((key) => {
            let value = accountInfo[key];
            let account: AccountInformation = {applicationName: applicationName, asset: key, balance: value };
            accountInformationList.push(account);
        });
        accountInformationContainerActions.updateAccountInformation(accountInformationList);
        HubClient.redux.dispatch({
            type: accountInformationContainerActions.type,
            payload: accountInformationContainerActions.payload,
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
    notifyMainCurrency(applicationName:string,currency:string){
        let exchangeUIContainerActions: ExchangeUIContainerActions.Actions = new ExchangeUIContainerActions.CRUDExchangeUIContainer(
            HubClient.exchangeUIContainer
        );
        let mainCurrency: MainCurrency = {
            application_name: applicationName,
            currency: currency
        }
        exchangeUIContainerActions.updateApplicationMainCurrency(mainCurrency);
        HubClient.redux.dispatch({
            type: exchangeUIContainerActions.type,
            payload: exchangeUIContainerActions.payload,
        });
    }
    notifyAssetInformation(applicationName:string,assetInformation: Record<string, AssetInformation>){
        let assetInformationContainerActions: AssetInformationContainerActions.Actions = new AssetInformationContainerActions.CRUDAssetInformationContainer(
            HubClient.assetInformationContainer
        );
        let assets: AssetInformation[] = new Array();
        let keyValuePairs = Object.keys(assetInformation);
        keyValuePairs.forEach((key) => {
            let value = assetInformation[key];
            let asset: AssetInformation = value;
            asset.id = key;
            asset.application_name = applicationName;
            assets.push(asset);
        });
        assetInformationContainerActions.updateAssetInformation(assets);
        HubClient.redux.dispatch({
            type: assetInformationContainerActions.type,
            payload: assetInformationContainerActions.payload,
        });
    }
    notifyProductChange(applicationName:string, productInfoList: ProductInformation[]){
        let productInformationContainerActions: ProductInformationContainerActions.Actions = new ProductInformationContainerActions.CRUDProductInformationContainer(
            HubClient.productInformationContainer
        );
        //map
        let productInformationList: ProductInformation[] = productInfoList.map((productInformation: ProductInformation) => {
            let product: ProductInformation = {
                application_name: applicationName,
                id: productInformation.id,
                base_currency: productInformation.base_currency,
                base_max_size: productInformation.base_max_size,
                base_min_size: productInformation.base_min_size,
                quote_currency: productInformation.quote_currency,
                quote_increment: productInformation.quote_increment
            };
            return product;
        });
        productInformationContainerActions.updateProductInformation(productInformationList);
        HubClient.redux.dispatch({
            type: productInformationContainerActions.type,
            payload: productInformationContainerActions.payload,
        });
    }
}
