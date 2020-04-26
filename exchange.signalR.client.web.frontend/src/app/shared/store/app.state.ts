import { NotificationContainer } from "@interfaces/notification-container.interface";
import * as NotificationContainerReducer from "@reducers/notification-container.reducer";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import * as ExchangeUIContainerReducer from "@reducers/exchange-ui-container.reducer";

export interface AppState {
  readonly notificationContainer: NotificationContainer;
  readonly exchangeUIContainer: ExchangeUIContainer;
}

export const INITIALSTATE: AppState = {
  notificationContainer: NotificationContainerReducer.initialState,
  exchangeUIContainer: ExchangeUIContainerReducer.initialState
};
