import { Action } from "redux";
import { DisplayContainer } from "@interfaces/display-container.interface";
import { Display } from "@interfaces/display.interface";

export const CRUDDISPLAYCONTAINER = "CRUDDISPLAYCONTAINER";

export class CRUDDisplayContainer implements Action {
    readonly type = CRUDDISPLAYCONTAINER;

    constructor(public payload: DisplayContainer) {
        this.payload = Object.assign({}, payload);
    }
    updateDisplayState(display: Display, productId: string, application: string) {
        this.payload.display = display;
        if(display.showFillsView){
            this.payload.selected_product_id = productId;
            this.payload.application_name = application;
        }else{
            this.payload.selected_product_id = undefined;
            this.payload.application_name = undefined;
        }
    }
}

export type Actions = CRUDDisplayContainer;
