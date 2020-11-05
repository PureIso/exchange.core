import { Price } from "@interfaces/price.interface";
import { MainCurrency } from "./main-currency.interface";

export interface ExchangeUIContainer {
    mainCurrencies: MainCurrency[];
    prices: Price[];
    applicationNames: string[];
}