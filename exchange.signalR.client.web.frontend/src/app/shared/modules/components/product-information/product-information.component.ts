import { Component,OnInit,Input,ViewChild,AfterViewInit } from "@angular/core";
import { NgRedux, select } from "@angular-redux/store";
import { AppState } from "@store/app.state";
import { Observable } from "rxjs";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { MainService } from "@services/main.service";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { FormControl } from '@angular/forms';
import { ProductInfo } from "@interfaces/product-info.interface";
import { AccountInfo } from "@interfaces/account-info.interface";

@Component({
    selector: "product-information-component",
    templateUrl: "./product-information.component.html",
})
export class ProductInformationComponent implements AfterViewInit, OnInit {
    @Input() applicationName: string;
    @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;
    @select("exchangeUIContainer") exchangeUIContainer$: Observable<ExchangeUIContainer>;
    exchangeUIContainer: ExchangeUIContainer;
    formControl: FormControl;
    quoteCurrencies: string[];
    assetList: string[];
    currentAssetList: string[];

    constructor(private ngRedux: NgRedux<AppState>, private mainService: MainService) {
        this.formControl = new FormControl();
        this.assetList = new Array();
        this.currentAssetList = new Array();
        this.quoteCurrencies = new Array();
    }

    ngOnInit() {
        this.notificationContainer$.subscribe((x: NotificationContainer) => {
            this.notificationContainer = x;
        });
        this.exchangeUIContainer$.subscribe((x: ExchangeUIContainer) => {
            this.exchangeUIContainer = x;
            this.assetList = new Array();
            this.currentAssetList = new Array();
            this.quoteCurrencies = new Array();

            //requires ES6 support, Babel or TypeScript
            x.productInfo.forEach((productInfo: ProductInfo)=>{
                if(productInfo.application_name == this.applicationName){
                    this.assetList.push(productInfo.id);
                    this.quoteCurrencies.push(productInfo.quote_currency);

                    x.accountInfo.forEach((accountInfo: AccountInfo) => {
                        if(accountInfo.applicationName == this.applicationName){
                            if(productInfo.id.indexOf(accountInfo.asset) !== -1)
                            {
                                let index: number = this.currentAssetList.findIndex((asset: string) => {
                                    return asset === productInfo.id;
                                });
                                if(index === -1){
                                    this.currentAssetList.push(productInfo.id);
                                }
                            } 
                        }
                    });
                };
                //filter - duplicates
                this.quoteCurrencies = this.quoteCurrencies.filter((quotedCurrency:string, index:number, quoteCurrencies:string[]) => {
                    return quoteCurrencies.indexOf(quotedCurrency) === index;
                });
            });
        });
    }

    ngAfterViewInit() {
        this.mainService.hub_requestedProducts();
    }
    //TODO
    onFilterAssets(){
        if(this.formControl.value == undefined || this.formControl.value == [])
            return;
        let currentValues: string[] = this.formControl.value;
        let filteredAssetList: ProductInfo[] = this.exchangeUIContainer.productInfo.filter((productInfo:ProductInfo) => {
            return currentValues.includes(productInfo.quote_currency);
        });
        filteredAssetList.forEach((productInfo: ProductInfo)=>{
            if(productInfo.application_name == this.applicationName){
                this.assetList.push(productInfo.id);
            }
        });
    }
    subscribe(){
        this.mainService.hub_requestedSubscription(this.applicationName,this.formControl.value)
    }
}
