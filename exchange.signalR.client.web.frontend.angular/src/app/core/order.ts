export interface Order {
  readonly id: string;
  readonly size: string;
  readonly product_id: string;
  readonly side: string;
  readonly stp: string;
  readonly fund: string;
  readonly type: string;
  readonly post_only: boolean;
  readonly price: string;
  readonly time_in_force: string;
  readonly created_at: string;
  readonly fill_fees: string;
  readonly filled_size: string;
  readonly executed_value: string;
  readonly status: string;
  readonly settled: boolean;
}
