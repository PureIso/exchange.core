import { Action } from "redux";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { Price } from "@interfaces/price.interface";
import { AccountInfo } from "@interfaces/account-info.interface";
import { ProductInfo } from "@interfaces/product-info.interface";
import { MainCurrency } from "@interfaces/main-currency.interface";
import { AssetInformation } from "@interfaces/asset-information.interface";

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
    private getCurrencyIndex(mainCurrency: MainCurrency): number {
        return this.payload.mainCurrencies.findIndex((x: MainCurrency) => {
            return x.application_name === mainCurrency.application_name;
        });
    }
    private getAssetInfoIndex(assetInfo: AssetInformation): number {
        return this.payload.assetInformation.findIndex((x: AssetInformation) => {
            return x.id === assetInfo.id && x.application_name === assetInfo.application_name;
        });
    }
    private getProductInfoIndex(productInfo: ProductInfo): number {
        return this.payload.productInfo.findIndex((x: ProductInfo) => {
            return x.id === productInfo.id && x.application_name === productInfo.application_name;
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
    updateApplicationMainCurrency(mainCurrency:MainCurrency) {
        this.payload.mainCurrencies.forEach((x:MainCurrency) => {
            let index = this.getCurrencyIndex(x);
            if (index == -1) {
                this.payload.mainCurrencies.push(mainCurrency);
            } else {
                this.payload.mainCurrencies[index] = mainCurrency;
            }
        });
        // console.log(this.payload.mainCurrencies);
    }
    updateAssetInformation(assetInformationList:AssetInformation[]){
        assetInformationList.forEach((assetInformation:AssetInformation) => {
            let index = this.getAssetInfoIndex(assetInformation);
            if (index == -1) {
                if(assetInformation != undefined){
                    this.payload.assetInformation.push(assetInformation);
                }
            } else {
                if(assetInformation != undefined){
                    this.payload.assetInformation[index] = assetInformation;
                    console.log( this.payload.assetInformation[index] );
                }
            }
        });
        //sort
        this.payload.assetInformation.sort((assetInformation1:AssetInformation, assetInformation2:AssetInformation) => {
            return this.sort(assetInformation1.id,assetInformation2.id);
        });
    }
    updateProductInfo(productInfoList:ProductInfo[]) {  
        productInfoList.forEach((productInfo:ProductInfo) => {
            let index = this.getProductInfoIndex(productInfo);
            if (index == -1) {
                this.payload.productInfo.push(productInfo);
            } else {
                this.payload.productInfo[index] = productInfo;
            }
        });
        //sort
        this.payload.productInfo.sort((productInfo1:ProductInfo, productInfo2:ProductInfo) => {
            return this.sort(productInfo1.id,productInfo2.id);
        });
    }
}

export type Actions = CRUDExchangeUIContainer;
