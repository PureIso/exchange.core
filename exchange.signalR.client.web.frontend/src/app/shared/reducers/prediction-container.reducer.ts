import * as PredictionContainerActions from "@actions/prediction-container.actions";
import { PredictionContainer } from "@interfaces/prediction-container.interface";

export const initialState: PredictionContainer = {
    result: {
        state: "N/A",
        status: {
            current_progress: 0,
            total_progress: 100,
            status: "N/A",
            close_price: {
                name: "N/A",
                series: []
            },
            close_price_prediction: {
                name: "N/A",
                series: []
            }
        }
    },
    task_status: {
        status: "",
        task_id: ""
    }
};
/**
 * Prediction Container Reducer
 * @param state The previous state
 * @param action The action that determines the next state
 */
export function predictionContainerReducer(state: PredictionContainer = initialState, action: PredictionContainerActions.Actions): PredictionContainer {
    if (action != null && action.type == PredictionContainerActions.CRUDPREDICTIONCONTAINER) {
        return action.payload;
    }
    return state;
}
