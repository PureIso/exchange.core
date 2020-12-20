import { MiniFill } from "./mini-fill.interface";

export interface FillStatistics {
    application_name: string,
    product_id: string,
    quote_currency: string,
    base_currency: string,
    mini_fill_sell_above_list: MiniFill[],
    mini_fill_buy_below_list: MiniFill[]
}