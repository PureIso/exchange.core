import { Action } from "redux";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { Price } from "@interfaces/price.interface";
export const CRUDEXCHANGEUICONTAINER = "CRUDEXCHANGEUICONTAINER";

export class CRUDExchangeUIContainer implements Action {
    readonly type = CRUDEXCHANGEUICONTAINER;

    constructor(public payload: ExchangeUIContainer) {
        this.payload = Object.assign({}, payload);
    }
    private getIndex(price: Price): number {
        return this.payload.prices.findIndex((x: Price) => {
            return x.asset === price.asset && x.applicationName === price.applicationName;
        });
    }
    updatePrices(prices: Price[]) {
        prices.forEach((price) => {
            let index = this.getIndex(price);
            if (index == -1) {
                this.payload.prices.push(price);
            } else {
                this.payload.prices[index] = price;
            }
        });
    }
}

export type Actions = CRUDExchangeUIContainer;
