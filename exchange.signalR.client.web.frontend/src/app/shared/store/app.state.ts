import { NotificationContainer } from "@interfaces/notification-container.interface";
import * as NotificationContainerReducer from "@reducers/notification-container.reducer";

export interface AppState {
  readonly notificationContainer: NotificationContainer;
}

export const INITIALSTATE: AppState = {
  notificationContainer: NotificationContainerReducer.initialState
};
