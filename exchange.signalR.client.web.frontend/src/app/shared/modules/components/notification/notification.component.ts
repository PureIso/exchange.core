import { Component, OnInit } from "@angular/core";
import { NgRedux, select } from "@angular-redux/store";
import { Observable } from "rxjs";
import * as NotificationActions from "@actions/notification-container.actions";
import { AppState } from "@store/app.state";
import { NotificationContainer } from "@interfaces/notification-container.interface";

@Component({
    selector: "notification-component",
    templateUrl: "./notification.component.html"
})
export class NotificationComponent implements OnInit {
    @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;

    constructor(private ngRedux: NgRedux<AppState>) { }

    ngOnInit() {
        this.notificationContainer$.subscribe(
            (notificationContainer: NotificationContainer) => (this.notificationContainer = notificationContainer)
        );
    }

    dismiss(id: string) {
        let notificationContainerActions: NotificationActions.Actions = new NotificationActions.CRUDNotificationContainer(this.notificationContainer);
        notificationContainerActions.removeNotification(id);
        this.ngRedux.dispatch({ type: notificationContainerActions.type, payload: notificationContainerActions.payload });
    }
}
