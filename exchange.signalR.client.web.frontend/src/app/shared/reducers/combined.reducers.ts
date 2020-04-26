import { combineReducers, createStore } from "redux";
import { notificationContainerReducer } from "./notification-container.reducer";
import { AppState, INITIALSTATE } from "../store/app.state";
import { exchangeUIContainerReducer } from "./exchange-ui-container.reducer";

export const combinedReducers = combineReducers<AppState>({
  notificationContainer: notificationContainerReducer,
  exchangeUIContainer: exchangeUIContainerReducer
});
export const store = createStore(combinedReducers, INITIALSTATE);
