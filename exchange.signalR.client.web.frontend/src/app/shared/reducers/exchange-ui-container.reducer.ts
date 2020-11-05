import * as ExchangeUIContainerActions from "@actions/exchange-ui-container.actions";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";

export const initialState: ExchangeUIContainer = {
    prices: [],
    applicationNames: [],
    mainCurrencies: []
};
/**
 * Exchange UI Container Reducer
 * @param state The previous state
 * @param action The action that determines the next state
 */
export function exchangeUIContainerReducer(state: ExchangeUIContainer = initialState, action: ExchangeUIContainerActions.Actions): ExchangeUIContainer {
    if (action != null && action.type == ExchangeUIContainerActions.CRUDEXCHANGEUICONTAINER) {
        return action.payload;
    }
    return state;
}
