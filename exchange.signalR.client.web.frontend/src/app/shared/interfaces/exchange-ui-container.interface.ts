import { Price } from "@interfaces/price.interface";
import { AccountInfo } from "./account-info.interface";

export interface ExchangeUIContainer {
    prices: Price[];
    accountInfo: AccountInfo[];
}