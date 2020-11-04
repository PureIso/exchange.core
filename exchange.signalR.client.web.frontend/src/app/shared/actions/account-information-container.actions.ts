import { Action } from "redux";
import { AccountInformationContainer } from "@interfaces/account-information-container.interface";
import { AccountInformation } from "@interfaces/account-information.interface";

export const CRUDACCOUNTINFORMATIONCONTAINER = "CRUDACCOUNTINFORMATIONCONTAINER";

export class CRUDAccountInformationContainer implements Action {
    readonly type = CRUDACCOUNTINFORMATIONCONTAINER;

    constructor(public payload: AccountInformationContainer) {
        this.payload = Object.assign({}, payload);
    }
    private getAccountInformationIndex(accountInformation: AccountInformation): number {
        return this.payload.accountInfo.findIndex((currentAccountInformation: AccountInformation) => {
            return currentAccountInformation.asset === accountInformation.asset && 
            currentAccountInformation.applicationName === accountInformation.applicationName;
        });
    }
    private sort(string1:string,string2:string):number{
        if (string1 > string2)
            return 1;
        else if (string1 < string2)
            return -1;
        return 0;
    }
    updateAccountInformation(accountInformationList: AccountInformation[]) {
        accountInformationList.forEach((accountInformation: AccountInformation) => {
            let index = this.getAccountInformationIndex(accountInformation);
            if (index == -1) {
                this.payload.accountInfo.push(accountInformation);
            } else {
                this.payload.accountInfo[index] = accountInformation;
            }
        });
        //sort
        this.payload.accountInfo.sort((accountInformation1:AccountInformation, accountInformation2:AccountInformation) => {
            return this.sort(accountInformation1.asset,accountInformation2.asset);
        });
    }
}

export type Actions = CRUDAccountInformationContainer;
