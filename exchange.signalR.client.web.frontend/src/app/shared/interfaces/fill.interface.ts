export interface Fill {
    application_name: string,
    trade_id: number,
    product_id: string,
    price: string,
    size: string,
    order_id: string,
    side: string,
    fee: string,
    settled: boolean,
    created_at: Date
}