import { Action } from "redux";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { Price } from "@interfaces/price.interface";
export const CRUDEXCHANGEUICONTAINER = "CRUDEXCHANGEUICONTAINER";

export class CRUDExchangeUIContainer implements Action {
  readonly type = CRUDEXCHANGEUICONTAINER;

  constructor(public payload: ExchangeUIContainer) {
    this.payload = Object.assign({}, this.payload);
  }

  updatePrices(price: Price) {
    this.payload.prices.push(price);
  }
}

export type Actions = CRUDExchangeUIContainer;
