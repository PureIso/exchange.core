import { Component,OnInit,Input,AfterViewInit } from "@angular/core";
import { select } from "@angular-redux/store";
import { Observable } from "rxjs";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { MainService } from "@services/main.service";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { FormControl } from '@angular/forms';
import { ProductInformation } from "@interfaces/product-information.interface";
import { AccountInformation } from "@interfaces/account-information.interface";
import { ProductInformationContainer } from "@interfaces/product-information-container.interface";
import { AccountInformationContainer } from "@interfaces/account-information-container.interface";

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
    @select("productInformationContainer") productInformationContainer$: Observable<ProductInformationContainer>;
    productInformationContainer: ProductInformationContainer;
    @select("accountInformationContainer") accountInformationContainer$: Observable<AccountInformationContainer>;
    accountInformationContainer: AccountInformationContainer;
    //
    productListFormControl: FormControl;
    quoteCurrenciesFormControl: FormControl;
    currentProductListFormControl: FormControl;
    //
    quoteCurrencies: string[];
    productList: string[];
    currentProductList: string[];
    masterProductList: string[];
    masterCurrentProductList: string[];
    //
    selectedCurrencies: string[];
    subscribeIsDisabled: boolean;

    constructor(private mainService: MainService) {
        this.productListFormControl = new FormControl({value: '', disabled: true});
        this.quoteCurrenciesFormControl = new FormControl();
        this.currentProductListFormControl = new FormControl();
        //
        this.quoteCurrencies = new Array();
        this.productList = new Array();
        this.currentProductList = new Array();
        this.masterCurrentProductList = new Array();
        this.masterProductList = new Array();
        //
        this.selectedCurrencies = new Array();
        this.subscribeIsDisabled = true;
    }
    ngOnInit() {
        this.notificationContainer$.subscribe((x: NotificationContainer) => {
            this.notificationContainer = x;
        });
        this.productInformationContainer$.subscribe((x: ProductInformationContainer) => {
            this.productInformationContainer = x;
            this.productInformationContainer.productInfo.forEach((productInformation: ProductInformation)=>{
                if(productInformation.application_name != this.applicationName)
                    return;
                let index: number = this.productList.findIndex((productId: string) => {
                    return productInformation.id === productId;
                });
                if(index === -1){
                    this.productList.push(productInformation.id);
                }
                //quotedCurrency = EUR      
                index = this.quoteCurrencies.findIndex((quotedCurrency: string) => {
                    return productInformation.quote_currency === quotedCurrency;
                });
                if(index === -1){
                    this.quoteCurrencies.push(productInformation.quote_currency);
                }
                this.productListFormControl.enable();
            });
            //set master record
            this.masterProductList = this.productList;
            this.masterProductList.sort((value1:string, value2:string) => {
                return this.sort(value1,value2);
            });
        });
        this.accountInformationContainer$.subscribe((x: AccountInformationContainer) => {
            this.accountInformationContainer = x;
            if(this.productList == undefined || this.productList == [])
                return;
            this.accountInformationContainer.accountInfo.forEach((accountInformation: AccountInformation)=>{
                if(accountInformation.applicationName != this.applicationName)
                    return;
                //list of products
                this.productList.forEach((productId: string)=>{
                    let index: number = this.quoteCurrencies.findIndex((quotedCurrency: string) => {
                        return productId === quotedCurrency;
                    });
                    if(index === -1){
                        this.quoteCurrencies.push(productId);
                    }
                    if(this.quoteCurrenciesFormControl.value != undefined && this.quoteCurrenciesFormControl.value != []){
                        let quoteCurrencies: string[] = this.quoteCurrenciesFormControl.value;
                        quoteCurrencies.forEach((quotedCurrency: string) =>{
                            if(!productId.startsWith(accountInformation.asset) && !productId.endsWith(quotedCurrency))
                                return;
                            index = this.currentProductList.findIndex((asset: string) => {
                                return asset === productId;
                            });
                            if(index === -1){
                                this.currentProductList.push(productId);
                            }
                        });
                    }
                    else{
                        if(!productId.startsWith(accountInformation.asset))
                            return;
                        index = this.currentProductList.findIndex((asset: string) => {
                            return asset === productId;
                        });
                        if(index === -1){
                            this.currentProductList.push(productId);
                        }
                    }
                    this.productListFormControl.enable();
                });  
            }); 
            //set master record
            this.masterCurrentProductList = this.currentProductList;   
            this.masterCurrentProductList.sort((value1:string, value2:string) => {
                return this.sort(value1,value2);
            });  
        });
    }
    ngAfterViewInit() {
        this.mainService.hub_requestedProducts();
    }
    private sort(string1:string,string2:string):number{
        if (string1 > string2)
            return 1;
        else if (string1 < string2)
            return -1;
        return 0;
    }
    onUpdateSubscribeProductList(){
        this.quoteCurrenciesFormControl.disable();
        this.productListFormControl.disable();
        this.currentProductListFormControl.disable();
        this.subscribeIsDisabled = true;
        //
        this.selectedCurrencies = new Array();
        if(this.productListFormControl.value != undefined && this.productListFormControl.value != [])
            this.selectedCurrencies = this.selectedCurrencies.concat(this.productListFormControl.value)
        if(this.currentProductListFormControl.value != undefined && this.currentProductListFormControl.value != [])
            this.selectedCurrencies = this.selectedCurrencies.concat(this.currentProductListFormControl.value)
        if(this.selectedCurrencies.length == 0){
            this.subscribeIsDisabled = false;
            this.quoteCurrenciesFormControl.enable();
            this.productListFormControl.enable();
            this.currentProductListFormControl.enable();
            return;
        }
        this.subscribeIsDisabled = false;
        this.quoteCurrenciesFormControl.enable();
        this.productListFormControl.enable();
        this.currentProductListFormControl.enable();
    }
    onFilteredProductList(){
        if(this.quoteCurrenciesFormControl.value == undefined || this.quoteCurrenciesFormControl.value == [])
            return;
        this.quoteCurrenciesFormControl.disable();
        this.productListFormControl.disable();
        this.currentProductListFormControl.disable();
        this.subscribeIsDisabled = true;
        //
        let filteredProductList: string[] = new Array();
        let filteredCurrentProductList: string[] = new Array();
        //
        let quoteCurrencies: string[] = this.quoteCurrenciesFormControl.value;
        quoteCurrencies.forEach((quotedCurrency: string) =>{
            //
            let tempFilteredProductList = this.masterProductList.filter((productId:string) => {
                if(!productId.endsWith(quotedCurrency))
                    return false; 
                return true;
            });
            filteredProductList = filteredProductList.concat(tempFilteredProductList);
            //
            let tempFilteredCurrentProductList = this.masterCurrentProductList.filter((currentProduct:string) => {
                if(!currentProduct.endsWith(quotedCurrency))
                    return false;
                return true;
            });
            filteredCurrentProductList = filteredCurrentProductList.concat(tempFilteredCurrentProductList);
        });
        this.productList = filteredProductList;
        this.currentProductList = filteredCurrentProductList;
        //
        this.productList.sort((value1:string, value2:string) => {
            return this.sort(value1,value2);
        });
        this.currentProductList.sort((value1:string, value2:string) => {
            return this.sort(value1,value2);
        });
        //
        this.subscribeIsDisabled = false;
        this.quoteCurrenciesFormControl.enable();
        this.productListFormControl.enable();
        this.currentProductListFormControl.enable();
    }
    subscribe(){
        if(this.selectedCurrencies.length == 0)
            return;
        this.mainService.hub_requestedSubscription(this.applicationName,this.selectedCurrencies)
    }
}
