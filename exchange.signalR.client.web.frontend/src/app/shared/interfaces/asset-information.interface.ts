import { PriceAndSize } from "./price-and-size.interface";

export interface AssetInformation {
    product_id: string;
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
    relative_index_daily: number;
    relative_index_hourly: number;
    base_currency_balance: number;
    base_currency_available: number;
    base_currency_hold: number;
    base_currency_symbol: string;
    quote_currency_balance: number;
    quote_currency_available: number;
    quote_currency_hold: number;
    quote_currency_symbol: string;
    base_and_quote_price: number;
    base_and_selected_main_price: number;
    base_and_quote_balance: number;
    base_and_selected_main_balance: number;
    selected_main_currency_balance: number;
    selected_main_currency_available: number;
    selected_main_currency_hold: number;
    selected_main_currency_symbol: string;
    aggregated_selected_main_balance: number;
}