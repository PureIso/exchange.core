import { Component, OnInit, Input, ViewChild, AfterViewInit } from "@angular/core";
import { NgRedux, select } from "@angular-redux/store";
import { AppState } from "@store/app.state";
import { Observable } from "rxjs";
import { NotificationContainer } from "@interfaces/notification-container.interface";

@Component({
    selector: "price-card-component",
    templateUrl: "./price-card.component.html"
})
export class PriceCardComponent implements AfterViewInit, OnInit {
    @Input() priceId: string;
    @Input() price: Number;
    @ViewChild('priceCard') priceCard:any;
    priceCardNativeElement: HTMLElement;

    @select('notificationContainer') notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;

    constructor(private ngRedux: NgRedux<AppState>) { }
    ngOnInit() {
        this.notificationContainer$.subscribe((x:NotificationContainer) =>{
            this.notificationContainer = x;
        });
    }

    ngAfterViewInit(){
        if(this.priceCard != undefined){
            this.priceCardNativeElement = this.priceCard.nativeElement as HTMLElement
        }
    }
}
