import { PriceAndSize } from "./price-and-size.interface";

export interface AssetInformation {
    id: string;
    application_name: string;
    twenty_four_hour_price_change: number;
    twenty_four_hour_price_percentage_change: number;
    currency_price: number;
    order_side: string;
    best_bid: string;
    best_ask: string;
    bid_max_order_size: number;
    index_of_max_bid_order_size: number;
    ask_max_order_size: number;
    index_of_max_ask_order_size: number;
    bid_price_and_size: PriceAndSize[];
    ask_price_and_size: PriceAndSize[];
    relative_index_quarterly: number;
    relative_index_daily:number;
    relative_index_hourly: number;
}