import { Action } from "redux";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { Price } from "@interfaces/price.interface";
import { AccountInfo } from "@interfaces/account-info.interface";

export const CRUDEXCHANGEUICONTAINER = "CRUDEXCHANGEUICONTAINER";

export class CRUDExchangeUIContainer implements Action {
    readonly type = CRUDEXCHANGEUICONTAINER;

    constructor(public payload: ExchangeUIContainer) {
        this.payload = Object.assign({}, payload);
    }
    private getPriceIndex(price: Price): number {
        return this.payload.prices.findIndex((x: Price) => {
            return x.asset === price.asset && x.applicationName === price.applicationName;
        });
    }
    private getAccountIndex(accountInfo: AccountInfo): number {
        return this.payload.accountInfo.findIndex((x: AccountInfo) => {
            return x.asset === accountInfo.asset && x.applicationName === accountInfo.applicationName;
        });
    }
    updatePrices(prices: Price[]) {
        prices.forEach((price) => {
            let index = this.getPriceIndex(price);
            if (index == -1) {
                this.payload.prices.push(price);
            } else {
                this.payload.prices[index] = price;
            }
        });
    }
    updateAccountInfo(accountInfo: AccountInfo[]) {
        accountInfo.forEach((account) => {
            let index = this.getAccountIndex(account);
            if (index == -1) {
                this.payload.accountInfo.push(account);
            } else {
                this.payload.accountInfo[index] = account;
            }
        });
    }
}

export type Actions = CRUDExchangeUIContainer;
