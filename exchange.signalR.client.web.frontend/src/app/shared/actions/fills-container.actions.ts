import { Action } from "redux";
import { FillsContainer } from "@interfaces/fills-container.interface";
import { Fill } from "@interfaces/fill.interface";
import { FillStatistics } from "@interfaces/fill-statistics.interface";

export const CRUDFILLSCONTAINER = "CRUDFILLSCONTAINER";

export class CRUDFillsContainer implements Action {
    readonly type = CRUDFILLSCONTAINER;

    constructor(public payload: FillsContainer) {
        this.payload = Object.assign({}, payload);
    }
    private getFillIndex(fill: Fill): number {
        return this.payload.fills.findIndex((currentFill: Fill) => {
            return currentFill.trade_id === fill.trade_id && 
            currentFill.application_name === fill.application_name;
        });
    }
    private reverse_sort(date1:Date,date2:Date):number {
        if (date1 === date2) {
            return 0;
        }
        return (date1 > date2) ? -1 : 1
    }
    updateFills(newFills: Fill[]) {
        newFills.forEach((fill: Fill) => {
            let index = this.getFillIndex(fill);
            if (index == -1) {
                this.payload.fills.push(fill);
            } else {
                this.payload.fills[index] = fill;
            }
        });
        //sort
        this.payload.fills.sort((fill1:Fill, fill2:Fill) => {
            return this.reverse_sort(fill1.created_at,fill2.created_at);
        });
    }
    updateFillStatistics(newFillStatistics: FillStatistics) {
        this.payload.fill_statistics = newFillStatistics;
    }
}

export type Actions = CRUDFillsContainer;
