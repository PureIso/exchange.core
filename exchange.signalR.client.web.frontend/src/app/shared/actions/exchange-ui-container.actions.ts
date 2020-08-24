import { Action } from "redux";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { Price } from "@interfaces/price.interface";
import { AccountInfo } from "@interfaces/account-info.interface";
import { ProductInfo } from "@interfaces/product-info.interface";

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
    private getAccountInfoIndex(accountInfo: AccountInfo): number {
        return this.payload.accountInfo.findIndex((x: AccountInfo) => {
            return x.asset === accountInfo.asset && x.applicationName === accountInfo.applicationName;
        });
    }
    private getProductInfoIndex(productInfo: ProductInfo): number {
        return this.payload.productInfo.findIndex((x: ProductInfo) => {
            return x.asset === productInfo.asset && x.applicationName === productInfo.applicationName;
        });
    }
    private getApplicationNameIndex(applicationName: string): number {
        return this.payload.applicationNames.findIndex((x: string) => {
            return x === applicationName;
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
            let index = this.getAccountInfoIndex(account);
            if (index == -1) {
                this.payload.accountInfo.push(account);
            } else {
                this.payload.accountInfo[index] = account;
            }
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
    }
    updateProductInfo(productInfo:ProductInfo) {
        let index = this.getProductInfoIndex(productInfo);
        if (index == -1) {
            this.payload.productInfo.push(productInfo);
        } else {
            this.payload.productInfo[index] = productInfo;
        }
    }
}

export type Actions = CRUDExchangeUIContainer;
