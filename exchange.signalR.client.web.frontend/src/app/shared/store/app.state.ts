import { NotificationContainer } from "@interfaces/notification-container.interface";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { ProductInformationContainer } from "@interfaces/product-information-container.interface";
import { AccountInformationContainer } from "@interfaces/account-information-container.interface";
import { AssetInformationContainer } from "@interfaces/asset-information-container.interface";
import * as NotificationContainerReducer from "@reducers/notification-container.reducer";
import * as ExchangeUIContainerReducer from "@reducers/exchange-ui-container.reducer";
import * as ProductInformationContainerReducer from "@reducers/product-information-container.reducer";
import * as AccountInformationContainerReducer from "@reducers/account-information-container.reducer";
import * as AssetInformationContainerReducer from "@reducers/asset-information-container.reducer";

export interface AppState {
    readonly notificationContainer: NotificationContainer;
    readonly exchangeUIContainer: ExchangeUIContainer;
    readonly productInformationContainer: ProductInformationContainer;
    readonly accountInformationContainer: AccountInformationContainer;
    readonly assetInformationContainer: AssetInformationContainer
}

export const INITIALSTATE: AppState = {
    notificationContainer: NotificationContainerReducer.initialState,
    exchangeUIContainer: ExchangeUIContainerReducer.initialState,
    productInformationContainer: ProductInformationContainerReducer.initialState,
    accountInformationContainer: AccountInformationContainerReducer.initialState,
    assetInformationContainer: AssetInformationContainerReducer.initialState
};
