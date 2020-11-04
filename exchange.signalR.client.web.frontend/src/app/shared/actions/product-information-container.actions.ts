import { Action } from "redux";
import { ProductInformationContainer } from "@interfaces/product-information-container.interface";
import { ProductInformation } from "@interfaces/product-information.interface";

export const CRUDPRODUCTINFORMATIONCONTAINER = "CRUDPRODUCTINFORMATIONCONTAINER";

export class CRUDProductInformationContainer implements Action {
    readonly type = CRUDPRODUCTINFORMATIONCONTAINER;

    constructor(public payload: ProductInformationContainer) {
        this.payload = Object.assign({}, payload);
    }
    private getProductInformationIndex(productInformation: ProductInformation): number {
        return this.payload.productInfo.findIndex((currentAccountInformation: ProductInformation) => {
            return currentAccountInformation.id === productInformation.id && 
            currentAccountInformation.application_name === productInformation.application_name;
        });
    }
    private sort(string1:string,string2:string):number{
        if (string1 > string2)
            return 1;
        else if (string1 < string2)
            return -1;
        return 0;
    }
    updateProductInformation(productInformationList: ProductInformation[]) {
        productInformationList.forEach((productInformation: ProductInformation) => {
            let index = this.getProductInformationIndex(productInformation);
            if (index == -1) {
                this.payload.productInfo.push(productInformation);
            } else {
                this.payload.productInfo[index] = productInformation;
            }
        });
        //sort
        this.payload.productInfo.sort((productInformation1:ProductInformation, productInformation2:ProductInformation) => {
            return this.sort(productInformation1.id,productInformation2.id);
        });
    }
}

export type Actions = CRUDProductInformationContainer;
