import { AccountInformation } from "./account-information.interface";
import { AnalyticsData } from "./analytics-data.interface";

export interface AnalyticsDataContainer {
    indicators: string[];
    analytics_data: AnalyticsData
}