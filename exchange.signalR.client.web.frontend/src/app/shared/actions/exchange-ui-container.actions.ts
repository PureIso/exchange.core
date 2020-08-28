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
    updateAccountInfo(accountInfo: AccountInfo[]) {
        accountInfo.forEach((account: AccountInfo) => {
            let index = this.getAccountInfoIndex(account);
            if (index == -1) {
                this.payload.accountInfo.push(account);
            } else {
                this.payload.accountInfo[index] = account;
            }
        });
        //sort
        this.payload.accountInfo.sort((accountInfo1:AccountInfo, accountInfo2:AccountInfo) => {
            return this.sort(accountInfo1.asset,accountInfo2.asset);
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
    updateProductInfo(productInfo:ProductInfo) {
        let index = this.getProductInfoIndex(productInfo);
        if (index == -1) {
            this.payload.productInfo.push(productInfo);
        } else {
            this.payload.productInfo[index] = productInfo;
        }
        //sort
        this.payload.productInfo.sort((productInfo1:ProductInfo, productInfo2:ProductInfo) => {
            return this.sort(productInfo1.asset,productInfo2.asset);
        });
    }
}

export type Actions = CRUDExchangeUIContainer;
