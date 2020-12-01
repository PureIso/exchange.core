import * as OrdersContainerActions from "@actions/orders-container.actions";
import { OrdersContainer } from "@interfaces/orders-container.interface";

export const initialState: OrdersContainer = {
    orders: [],
};
/**
 * Fill Container Reducer
 * @param state The previous state
 * @param action The action that determines the next state
 */
export function ordersContainerReducer(state: OrdersContainer = initialState, action: OrdersContainerActions.Actions): OrdersContainer {
    if (action != null && action.type == OrdersContainerActions.CRUDORDERSCONTAINER) {
        return action.payload;
    }
    return state;
}
