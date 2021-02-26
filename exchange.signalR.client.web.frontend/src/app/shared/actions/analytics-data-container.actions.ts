import { Action } from "redux";
import { AnalyticsDataContainer } from "@interfaces/analytics-data-container";
import { IndicatorInformation } from "@interfaces/indicator-information";

export const CRUDANALYTICSDATACONTAINER = "CRUDANALYTICSDATACONTAINER";

export class CRUDAnalyticsDataContainer implements Action {
    readonly type = CRUDANALYTICSDATACONTAINER;

    constructor(public payload: AnalyticsDataContainer) {
        this.payload = Object.assign({}, payload);
    }
    private getIndicatorIndex(the_inidcator_name: string): number {
        return this.payload.indicator_information.indicator_files.findIndex((indicator_name: string) => {
            return indicator_name === the_inidcator_name;
        });
    }
    updateIndicatorNameList(indicator_information: IndicatorInformation) {
        indicator_information.indicator_files.forEach((the_inidcator_name: string) => {
            let index = this.getIndicatorIndex(the_inidcator_name);
            if (index == -1) {
                this.payload.indicator_information.indicator_files.push(the_inidcator_name);
            } else {
                this.payload.indicator_information.indicator_files[index] = the_inidcator_name;
            }
        });
    }
}

export type Actions = CRUDAnalyticsDataContainer;
