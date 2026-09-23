export interface Fill {
  readonly trade_id: number;
  readonly external_trade_id?: string | null;
  readonly product_id: string;
  readonly price: string;
  readonly size: string;
  readonly order_id: string;
  readonly side: string;
  readonly fee: string;
  readonly settled: boolean;
  readonly created_at: string;
}
