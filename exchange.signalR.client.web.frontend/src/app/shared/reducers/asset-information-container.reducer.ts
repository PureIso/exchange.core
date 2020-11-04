import * as AssettInformationContainerActions from "@actions/asset-information-container.actions";
import { AssetInformationContainer } from "@interfaces/asset-information-container.interface";

export const initialState: AssetInformationContainer = {
    assetInformation: []
};
/**
 * Asset Information Container Reducer
 * @param state The previous state
 * @param action The action that determines the next state
 */
export function assetInformationContainerReducer(state: AssetInformationContainer = initialState, action: AssettInformationContainerActions.Actions): AssetInformationContainer {
    if (action != null && action.type == AssettInformationContainerActions.CRUDASSETINFORMATIONCONTAINER) {
        return action.payload;
    }
    return state;
}
