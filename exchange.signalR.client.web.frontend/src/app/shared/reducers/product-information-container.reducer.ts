import * as ProductInformationContainerActions from "@actions/product-information-container.actions";
import { ProductInformationContainer } from "@interfaces/product-information-container.interface";

export const initialState: ProductInformationContainer = {
    productInfo: []
};
/**
 * Product Information Container Reducer
 * @param state The previous state
 * @param action The action that determines the next state
 */
export function productInformationContainerReducer(state: ProductInformationContainer = initialState, action: ProductInformationContainerActions.Actions): ProductInformationContainer {
    if (action != null && action.type == ProductInformationContainerActions.CRUDPRODUCTINFORMATIONCONTAINER) {
        return action.payload;
    }
    return state;
}
