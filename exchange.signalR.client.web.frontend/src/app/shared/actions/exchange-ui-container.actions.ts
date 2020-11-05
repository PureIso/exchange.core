import { Action } from "redux";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { Price } from "@interfaces/price.interface";
import { MainCurrency } from "@interfaces/main-currency.interface";

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
    private getCurrencyIndex(mainCurrency: MainCurrency): number {
        return this.payload.mainCurrencies.findIndex((x: MainCurrency) => {
            return x.application_name === mainCurrency.application_name;
        });
    }
    private getApplicationNameIndex(applicationName: string): number {
        return this.payload.applicationNames.findIndex((x: string) => {
            return x === applicationName;
        });
    }
    private sort(string1:string,string2:string):number{
        if (string1 > string2)
            return 1;
        else if (string1 < string2)
            return -1;
        return 0;
    }

    public updatePrices(prices: Price[]) {
        prices.forEach((price: Price) => {
            let index = this.getPriceIndex(price);
            if (index == -1) {
                this.payload.prices.push(price);
            } else {
                this.payload.prices[index] = price;
            }
        });
        //sort
        this.payload.prices.sort((price1:Price, price2:Price) => {
            return this.sort(price1.asset,price2.asset);
        });
    }
    updateApplicationNames(applicationNames:string[]) {
        applicationNames.forEach((applicationName:string) => {
            let index = this.getApplicationNameIndex(applicationName);
            if (index == -1) {
                this.payload.applicationNames.push(applicationName);
            } else {
                this.payload.applicationNames[index] = applicationName;
            }
        });
        //sort
        this.payload.applicationNames.sort((applicationName1:string, applicationName2:string) => {
            return this.sort(applicationName1,applicationName2)
        });
    }
    updateApplicationMainCurrency(mainCurrency:MainCurrency) {
        this.payload.mainCurrencies.forEach((x:MainCurrency) => {
            let index = this.getCurrencyIndex(x);
            if (index == -1) {
                this.payload.mainCurrencies.push(mainCurrency);
            } else {
                this.payload.mainCurrencies[index] = mainCurrency;
            }
        });
    }
}

export type Actions = CRUDExchangeUIContainer;
