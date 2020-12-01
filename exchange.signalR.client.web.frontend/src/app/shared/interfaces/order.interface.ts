export interface Order {
    application_name: string,
    id: string,
    size: string,
    product_id: string,
    side: string,
    price: string,
    stop_price: string,
    created_at: Date,
    fill_fee:string,
    fill_size: string
}