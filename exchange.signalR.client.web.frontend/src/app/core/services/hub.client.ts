import { AppState } from "@store/app.state";
import { NgRedux } from "@angular-redux/store";

export class HubClient{
    static redux:NgRedux<AppState>;

    constructor(){}
    setRedux(redux: NgRedux<AppState>){
        HubClient.redux = redux;
    }

    notifyCurrentPrices(prices: any){
        console.log(prices);
    }

    notifyInformation(messageType:any,message:string){
        console.log(messageType +" - "+message);
    }
}