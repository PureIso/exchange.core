import { Component, Input, OnInit, ViewChild } from "@angular/core";
import { NgRedux, select } from "@angular-redux/store";
import { AppState } from "@store/app.state";
import { Observable } from "rxjs";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { MainService } from "@services/main.service";
import { FillsContainer } from "@interfaces/fills-container.interface";
import { Fill } from "@interfaces/fill.interface";
import { DisplayContainer } from "@interfaces/display-container.interface";
import { Order } from "@interfaces/order.interface";
import { OrdersContainer } from "@interfaces/orders-container.interface";
import { AssetInformationContainer } from "@interfaces/asset-information-container.interface";
import { AssetInformation } from "@interfaces/asset-information.interface";
import { MatPaginator } from "@angular/material/paginator";
import { MatTableDataSource } from "@angular/material/table";

@Component({
    selector: "product-trade-component",
    templateUrl: "./product-trade.component.html",
    styleUrls: ["./product-trade.component.css"]
})
export class ProductTradeComponent implements OnInit {
    @Input() applicationName: string;
    @ViewChild(MatPaginator) paginator: MatPaginator;  
    columnsToDisplay = ['side', 'size', 'price', 'created_at', 'cancelOrder'];
    currentAssetColumnsToDisplay = ['base_currency_balance', 'quote_currency_balance', 'base_and_quote_balance', 
    'selected_main_currency_balance', 'base_and_selected_main_balance'];

    @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;
    @select("fillsContainer") fillsContainer$: Observable<FillsContainer>;
    fillsContainer: FillsContainer;
    @select("displayContainer") displayContainer$: Observable<DisplayContainer>;
    displayContainer: DisplayContainer;
    @select("ordersContainer") ordersContainer$: Observable<OrdersContainer>;
    ordersContainer: OrdersContainer;
    @select("assetInformationContainer") assetInformationContainer$: Observable<AssetInformationContainer>;
    assetInformationContainer: AssetInformationContainer;

    currentAssetInfo: AssetInformation;
    fills: Fill[];
    orders: Order[];
    assetInformation: AssetInformation[];
    dataSource = new MatTableDataSource();

    buyAmountMatInput: string;
    buyLimitPriceMatInput: string;
    buyTypeMatSelect: string;
    sellAmountMatInput: string;
    sellLimitPriceMatInput: string;
    sellTypeMatSelect: string;

    constructor(private ngRedux: NgRedux<AppState>, private mainService: MainService) {
        this.fills = new Array();
        this.assetInformation = new Array();
        this.dataSource.paginator = this.paginator;
        this.buyTypeMatSelect = 'limit';
        this.sellTypeMatSelect = 'limit';
    }

    ngOnInit() {
        this.notificationContainer$.subscribe((x: NotificationContainer) => {
            this.notificationContainer = x;
        });
        this.displayContainer$.subscribe((x: DisplayContainer) => {
            this.displayContainer = x;
            if (!x.display.showFillsView) {
                this.fills = new Array();
            }
        });
        this.fillsContainer$.subscribe((x: FillsContainer) => {
            this.fillsContainer = x;
            this.fills = new Array();
            if (x.fills.length > 0) {
                let fillList: Fill[] = x.fills.filter((fill: Fill) => {
                    return fill.application_name == this.applicationName &&
                        fill.product_id == this.displayContainer.selected_product_id
                });
                if (fillList != undefined) {
                    this.fills = fillList;
                }
            }
        });
        this.ordersContainer$.subscribe((x: OrdersContainer) => {
            this.ordersContainer = x;
            this.orders = new Array();
            if (x.orders.length > 0) {
                let orderList: Order[] = x.orders.filter((order: Order) => {
                    return order.application_name == this.applicationName &&
                        order.product_id == this.displayContainer.selected_product_id
                });
                if (orderList != undefined) {
                    this.orders = orderList;
                }
                this.dataSource = new MatTableDataSource(this.orders);
                this.dataSource.paginator = this.paginator;
            }
        });
        this.assetInformationContainer$.subscribe((x: AssetInformationContainer) => {
            this.assetInformationContainer = x;
            this.assetInformation = new Array();
            if (x.assetInformation.length > 0) {
                let index: number = x.assetInformation.findIndex((asset: AssetInformation) => {
                    return asset.application_name == this.applicationName &&
                        asset.product_id == this.displayContainer.selected_product_id
                });
                if (index !== -1) {
                    this.currentAssetInfo = x.assetInformation[index];
                    this.assetInformation.push(this.currentAssetInfo);
                }
            }
        });
    }
    getAskingBuyPrice() {
        this.buyLimitPriceMatInput = this.currentAssetInfo.best_ask;
    }
    getAskingSellPrice() {
        this.sellLimitPriceMatInput = this.currentAssetInfo.best_bid;
    }
    sellTrade() {
        let order: Order = {
            application_name: this.applicationName,
            side: "sell",
            size: this.sellAmountMatInput,
            price: this.sellLimitPriceMatInput,
            product_id: this.displayContainer.selected_product_id,
            created_at: new Date(),
            fill_fee: '',
            fill_size: '',
            id: '',
            stop_price: ''
        }
        this.mainService.hub_requestedPlaceOrder(this.applicationName, order);
    }
    buyTrade() {
        let order: Order = {
            application_name: this.applicationName,
            side: "buy",
            size: this.buyAmountMatInput,
            price: this.buyLimitPriceMatInput,
            product_id: this.displayContainer.selected_product_id,
            created_at: new Date(),
            fill_fee: '',
            fill_size: '',
            id: '',
            stop_price: ''
        }
        this.mainService.hub_requestedPlaceOrder(this.applicationName, order);
    }
    toggleCancelOrder(orderId: string) {
        this.mainService.hub_requestedCancelOrder(this.applicationName, orderId);
    }
    toggleCancelAllOrders() {
        this.mainService.hub_requestedCancelAllOrder(this.applicationName, this.displayContainer.selected_product_id);
    }
}