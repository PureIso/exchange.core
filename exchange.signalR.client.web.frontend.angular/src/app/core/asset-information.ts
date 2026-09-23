export interface AssetInformation {
  readonly product_id: string;
  readonly twenty_four_hour_price_change: number;
  readonly twenty_four_hour_price_percentage_change: number;
  readonly currency_price: number;
  readonly order_side: number | string;
  readonly best_bid: string;
  readonly best_ask: string;
  readonly base_currency_symbol: string;
  readonly quote_currency_symbol: string;
}
