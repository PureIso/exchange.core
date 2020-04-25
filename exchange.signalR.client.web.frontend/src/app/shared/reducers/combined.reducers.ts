import { combineReducers, createStore } from "redux";
import { notificationContainerReducer } from "./notification-container.reducer";
import { AppState, INITIALSTATE } from "../store/app.state";

export const combinedReducers = combineReducers<AppState>({
  notificationContainer: notificationContainerReducer
});
export const store = createStore(combinedReducers, INITIALSTATE);
