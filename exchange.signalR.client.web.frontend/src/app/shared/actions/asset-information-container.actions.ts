import { Action } from "redux";
import { AssetInformationContainer } from "@interfaces/asset-information-container.interface";
import { AssetInformation } from "@interfaces/asset-information.interface";

export const CRUDASSETINFORMATIONCONTAINER = "CRUDASSETINFORMATIONCONTAINER";

export class CRUDAssetInformationContainer implements Action {
    readonly type = CRUDASSETINFORMATIONCONTAINER;

    constructor(public payload: AssetInformationContainer) {
        this.payload = Object.assign({}, payload);
    }
    private getAssetInformationIndex(assetInformation: AssetInformation): number {
        return this.payload.assetInformation.findIndex((asset: AssetInformation) => {
            return asset.product_id === assetInformation.product_id && 
            asset.application_name === assetInformation.application_name;
        });
    }
    private sort(string1:string,string2:string):number{
        if (string1 > string2)
            return 1;
        else if (string1 < string2)
            return -1;
        return 0;
    }
    updateAssetInformation(accountInformationList: AssetInformation[]) {
        accountInformationList.forEach((accountInformation: AssetInformation) => {
            let index = this.getAssetInformationIndex(accountInformation);
            if (index == -1) {
                this.payload.assetInformation.push(accountInformation);
            } else {
                this.payload.assetInformation[index] = accountInformation;
            }
        });
        //sort
        this.payload.assetInformation.sort((accountInformation1:AssetInformation, accountInformation2:AssetInformation) => {
            return this.sort(accountInformation1.product_id,accountInformation2.product_id);
        });
    }
}

export type Actions = CRUDAssetInformationContainer;
