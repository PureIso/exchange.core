import { Action } from "redux";
import { PredictionContainer } from "@interfaces/prediction-container.interface";
import { PredictionResult } from "@interfaces/prediction-result.interface";
import { PredictionTaskStatus } from "@interfaces/prediction-task-status.interface";

export const CRUDPREDICTIONCONTAINER = "CRUDPREDICTIONCONTAINER";

export class CRUDPredictionContainer implements Action {
    readonly type = CRUDPREDICTIONCONTAINER;

    constructor(public payload: PredictionContainer) {
        this.payload = Object.assign({}, payload);
    }
    updatePredictionResult(prediction_result: PredictionResult) {
        this.payload.result = prediction_result;
    }
    updateTaskStatusResult(prediction_task_status: PredictionTaskStatus) {
        this.payload.task_status = prediction_task_status;
    }
}

export type Actions = CRUDPredictionContainer;
