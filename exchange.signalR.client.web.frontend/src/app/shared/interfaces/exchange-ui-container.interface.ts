import { Price } from "@interfaces/price.interface";
import { AccountInfo } from "./account-info.interface";
import { AssetInformation } from "./asset-information.interface";
import { MainCurrency } from "./main-currency.interface";
import { ProductInfo } from "./product-info.interface";

export interface ExchangeUIContainer {
    mainCurrencies: MainCurrency[];
    assetInformation: AssetInformation[];
    prices: Price[];
    accountInfo: AccountInfo[];
    applicationNames: string[];
    productInfo: ProductInfo[];
}