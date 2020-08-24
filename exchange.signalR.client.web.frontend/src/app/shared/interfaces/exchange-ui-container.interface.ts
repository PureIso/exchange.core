import { Price } from "@interfaces/price.interface";
import { AccountInfo } from "./account-info.interface";
import { ProductInfo } from "./product-info.interface";

export interface ExchangeUIContainer {
    prices: Price[];
    accountInfo: AccountInfo[];
    applicationNames: string[];
    productInfo: ProductInfo[];
}