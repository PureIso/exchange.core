import * as AnalyticsDataContainerActions from "@actions/analytics-data-container.actions";
import { AnalyticsDataContainer } from "@interfaces/analytics-data-container";

export const initialState: AnalyticsDataContainer = {
    indicator_information: {
        api_version: "",
        indicator_files: []
    },
    analytics_data: {
        indicator_name: ""
    }
};
/**
 * Analytics Data Container Reducer
 * @param state The previous state
 * @param action The action that determines the next state
 */
export function analyticsDataContainerReducer(state: AnalyticsDataContainer = initialState, action: AnalyticsDataContainerActions.Actions): AnalyticsDataContainer {
    if (action != null && action.type == AnalyticsDataContainerActions.CRUDANALYTICSDATACONTAINER) {
        return action.payload;
    }
    return state;
}
