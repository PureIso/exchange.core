import { Component,Input, OnInit, ViewChild } from "@angular/core";
import { NgRedux, select } from "@angular-redux/store";
import { AppState } from "@store/app.state";
import { Observable } from "rxjs";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { MainService } from "@services/main.service";
import { FillsContainer } from "@interfaces/fills-container.interface";
import { Fill } from "@interfaces/fill.interface";
import { DisplayContainer } from "@interfaces/display-container.interface";
import { MatPaginator } from "@angular/material/paginator";
import { MatTableDataSource } from '@angular/material/table';

@Component({
    selector: "product-trade-component",
    templateUrl: "./product-trade.component.html",
    styleUrls: ["./product-trade.component.css"]
})
export class ProductTradeComponent implements OnInit {
    @Input() applicationName: string;

    @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;
    @select("fillsContainer") fillsContainer$: Observable<FillsContainer>;
    fillsContainer: FillsContainer;
    @select("displayContainer") displayContainer$: Observable<DisplayContainer>;
    displayContainer: DisplayContainer;
    fills: Fill[];

    constructor(private ngRedux: NgRedux<AppState>, private mainService: MainService) {
        this.fills = new Array();
    }

    ngOnInit() {
        this.notificationContainer$.subscribe((x: NotificationContainer) => {
            this.notificationContainer = x;
        });
        this.displayContainer$.subscribe((x: DisplayContainer) => {
            this.displayContainer = x;
            console.log(this.displayContainer.selected_product_id);
            if(!x.display.showFillsView){
                this.fills = new Array();
            }
        });
        this.fillsContainer$.subscribe((x: FillsContainer) => {
            this.fillsContainer = x;
            this.fills = new Array();
            if(x.fills.length > 0){
                let fillList: Fill[] = x.fills.filter((fill:Fill) => {
                    return fill.application_name == this.applicationName;
                });
                if(fillList != undefined){
                    this.fills = fillList;
                }
            }
        });
    }
}