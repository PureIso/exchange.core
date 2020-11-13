import * as FillsContainerActions from "@actions/fills-container.actions";
import { FillsContainer } from "@interfaces/fills-container.interface";

export const initialState: FillsContainer = {
    fills: [],
};
/**
 * Fill Container Reducer
 * @param state The previous state
 * @param action The action that determines the next state
 */
export function fillsContainerReducer(state: FillsContainer = initialState, action: FillsContainerActions.Actions): FillsContainer {
    if (action != null && action.type == FillsContainerActions.CRUDFILLSCONTAINER) {
        return action.payload;
    }
    return state;
}
