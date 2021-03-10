import { PredictionResult } from "./prediction-result.interface";
import { PredictionTaskStatus } from "./prediction-task-status.interface";

export interface PredictionContainer {
    result: PredictionResult;
    task_status: PredictionTaskStatus;
}