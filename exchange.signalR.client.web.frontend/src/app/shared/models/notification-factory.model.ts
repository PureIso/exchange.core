import { Notification } from "@interfaces/notification.interface";

export class NotificationFactory {
    notification: Notification

    constructor() {
        //default value
        this.notification = {
            content: "",
            dismissed: false,
            id: "",
            style: ""
        }
    }

    create(content: string, dismissed: boolean, id: string, style: 'error' | 'success'): Notification {
        return this.notification = {
            content: content,
            dismissed: dismissed,
            id: id,
            style: style
        }
    }
}