import { Action } from "redux";
import { OrdersContainer } from "@interfaces/orders-container.interface";
import { Order } from "@interfaces/order.interface";

export const CRUDORDERSCONTAINER = "CRUDORDERSCONTAINER";

export class CRUDOrdersContainer implements Action {
    readonly type = CRUDORDERSCONTAINER;

    constructor(public payload: OrdersContainer) {
        this.payload = Object.assign({}, payload);
    }
    private getOrderIndex(order: Order): number {
        return this.payload.orders.findIndex((currentOrder: Order) => {
            return currentOrder.id === order.id && 
            currentOrder.application_name === order.application_name;
        });
    }
    private reverse_sort(date1:Date,date2:Date):number {
        if (date1 === date2) {
            return 0;
        }
        return (date1 > date2) ? -1 : 1
    }
    updateOrders(newOrders: Order[]) {
        newOrders.forEach((order: Order) => {
            let index = this.getOrderIndex(order);
            if (index == -1) {
                this.payload.orders.push(order);
            } else {
                this.payload.orders[index] = order;
            }
        });
        //sort
        this.payload.orders.sort((order1:Order, order2:Order) => {
            return this.reverse_sort(order1.created_at,order2.created_at);
        });
    }
}

export type Actions = CRUDOrdersContainer;
