import { Display } from "./display.interface";

export interface DisplayContainer {
    display: Display;
    selected_product_id: string;
    application_name: string;
}