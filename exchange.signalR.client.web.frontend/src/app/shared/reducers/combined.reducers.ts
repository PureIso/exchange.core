import { combineReducers, createStore } from "redux";
import { AppState, INITIALSTATE } from "../store/app.state";
import { notificationContainerReducer } from "./notification-container.reducer";
import { exchangeUIContainerReducer } from "./exchange-ui-container.reducer";
import { productInformationContainerReducer } from "./product-information-container.reducer";
import { accountInformationContainerReducer } from "./account-information-container.reducer";
import { assetInformationContainerReducer } from "./asset-information-container.reducer";

export const combinedReducers = combineReducers<AppState>({
	notificationContainer: notificationContainerReducer,
	exchangeUIContainer: exchangeUIContainerReducer,
	productInformationContainer: productInformationContainerReducer,
	accountInformationContainer: accountInformationContainerReducer,
	assetInformationContainer: assetInformationContainerReducer
});
export const store = createStore(combinedReducers, INITIALSTATE);
