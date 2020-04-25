import { AppState } from "@store/app.state";
import { NgRedux } from "@angular-redux/store";

export class HubClient{
    static redux:NgRedux<AppState>;

    constructor(){}
    setRedux(redux: NgRedux<AppState>){
        HubClient.redux = redux;
    }
}