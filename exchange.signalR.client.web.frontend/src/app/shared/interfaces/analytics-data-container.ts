import { AccountInformation } from "./account-information.interface";
import { AnalyticsData } from "./analytics-data.interface";
import { IndicatorInformation } from "./indicator-information";

export interface AnalyticsDataContainer {
    indicator_information: IndicatorInformation;
    analytics_data: AnalyticsData
}