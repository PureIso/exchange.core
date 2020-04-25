import { NotificationContainer } from "@interfaces/notification-container.interface";
import * as NotificationContainerActions from "@actions/notification-container.actions";

export const initialState: NotificationContainer = {
    notifications: []
};
/**
 * Notification Reducer
 * @param state The previous state
 * @param action The action that determines the next state
 */
export function notificationContainerReducer(state: NotificationContainer = initialState, action: NotificationContainerActions.Actions): NotificationContainer {
    if (action != null && action.type == NotificationContainerActions.CRUDNOTIFICATIONCONTAINER) {
        return action.payload;
    }
    return state;
}
