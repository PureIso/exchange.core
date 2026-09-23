export interface MiniFill {
  readonly price: number;
  readonly size: number;
}

export interface FillStatistics {
  readonly product_id: string;
  readonly quote_currency: string;
  readonly base_currency: string;
  readonly mini_fill_sell_above_list: readonly MiniFill[];
  readonly mini_fill_buy_below_list: readonly MiniFill[];
}
