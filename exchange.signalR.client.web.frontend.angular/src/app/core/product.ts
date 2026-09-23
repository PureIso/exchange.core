export interface Product {
  readonly id: string;
  readonly base_currency: string;
  readonly quote_currency: string;
  readonly base_min_size: string;
  readonly base_max_size: string;
  readonly quote_increment: string;
}
