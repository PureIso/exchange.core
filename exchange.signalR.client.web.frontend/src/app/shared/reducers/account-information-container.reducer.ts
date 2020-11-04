import * as AccountInformationContainerActions from "@actions/account-information-container.actions";
import { AccountInformationContainer } from "@interfaces/account-information-container.interface";

export const initialState: AccountInformationContainer = {
    accountInfo: []
};
/**
 * Account Information Container Reducer
 * @param state The previous state
 * @param action The action that determines the next state
 */
export function accountInformationContainerReducer(state: AccountInformationContainer = initialState, action: AccountInformationContainerActions.Actions): AccountInformationContainer {
    if (action != null && action.type == AccountInformationContainerActions.CRUDACCOUNTINFORMATIONCONTAINER) {
        return action.payload;
    }
    return state;
}
