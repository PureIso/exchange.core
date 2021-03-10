import { PredictionData } from "./prediction-data.interface";

export interface Prediction {
    current_progress: number,
    total_progress: number,
    status: string,
    close_price: PredictionData,
    close_price_prediction: PredictionData
}