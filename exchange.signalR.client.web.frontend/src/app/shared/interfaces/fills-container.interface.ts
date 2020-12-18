import { FillStatistics } from "./fill-statistics.interface";
import { Fill } from "./fill.interface";

export interface FillsContainer {
    fills: Fill[];
    fill_statistics: FillStatistics
}