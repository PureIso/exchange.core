import { Component,OnInit,Input,AfterViewInit } from "@angular/core";
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
    assetListFormControl: FormControl;
    quoteCurrenriesFormControl: FormControl;
    currentCurrenriesFormControl: FormControl;
    quoteCurrencies: string[];
    assetList: string[];
    currentAssetList: string[];

    constructor(private mainService: MainService) {
        this.assetListFormControl = new FormControl({value: '', disabled: true});
        this.quoteCurrenriesFormControl = new FormControl();
        this.currentCurrenriesFormControl = new FormControl();
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
    onFilterAssets(){
        if(this.quoteCurrenriesFormControl.value == undefined || this.quoteCurrenriesFormControl.value == [])
            return;
        this.assetList = new Array();
        let currentValues: string[] = this.quoteCurrenriesFormControl.value;
        let filteredAssetList: ProductInfo[] = this.exchangeUIContainer.productInfo.filter((productInfo:ProductInfo) => {
            if(productInfo.application_name == this.applicationName){
                return currentValues.includes(productInfo.quote_currency);
            }
            return false;
        });
        filteredAssetList.forEach((productInfo: ProductInfo)=>{          
            if(productInfo.application_name == this.applicationName){
                this.assetList.push(productInfo.id);
            }
            this.assetListFormControl.enable();
        });
    }
    subscribe(){
        let assets = new Array();
        if(this.assetListFormControl.value != undefined && this.assetListFormControl.value != [])
            assets = assets.concat(this.assetListFormControl.value)
        if(this.currentCurrenriesFormControl.value != undefined && this.currentCurrenriesFormControl.value != [])
            assets = assets.concat(this.currentCurrenriesFormControl.value)
        if(assets.length == 0)
            return;
        this.mainService.hub_requestedSubscription(this.applicationName,assets)
    }
}
