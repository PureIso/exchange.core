import * as DisplayContainerActions from "@actions/display-container.actions";
import { DisplayContainer } from "@interfaces/display-container.interface";

export const initialState: DisplayContainer = {
    display: { showFillsView: false},
    selected_product_id: undefined,
    application_name: undefined,
};
/**
 * Display Container Reducer
 * @param state The previous state
 * @param action The action that determines the next state
 */
export function displayContainerReducer(state: DisplayContainer = initialState, action: DisplayContainerActions.Actions): DisplayContainer {
    if (action != null && action.type == DisplayContainerActions.CRUDDISPLAYCONTAINER) {
        return action.payload;
    }
    return state;
}
