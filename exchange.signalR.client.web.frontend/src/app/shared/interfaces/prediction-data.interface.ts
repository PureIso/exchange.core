import { PredictionDataSeries } from "./prediction-data-series.interface";

export interface PredictionData {
    name: string,
    series: PredictionDataSeries[]
}