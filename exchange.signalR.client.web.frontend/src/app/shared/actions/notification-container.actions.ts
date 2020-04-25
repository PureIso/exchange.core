import { Action } from "redux";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { Notification } from "@interfaces/notification.interface";

export const CRUDNOTIFICATIONCONTAINER = "CRUDNOTIFICATIONCONTAINER";

/**
 * Add a new notification to the notification container
 */
export class CRUDNotificationContainer implements Action {
  /**
   * Notification action type
   */
  readonly type = CRUDNOTIFICATIONCONTAINER;

  constructor(public payload: NotificationContainer) {
    this.payload = Object.assign({}, this.payload);
  }

  addNotification(notification: Notification) {
    this.payload.notifications.push(notification);
  }

  removeNotification(id: string) {
    this.payload.notifications = this.payload.notifications.filter((notification: Notification) => {
      return notification.id !== id;
    });
  }
}

export type Actions = CRUDNotificationContainer;
