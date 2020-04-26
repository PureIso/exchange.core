import { Injectable } from "@angular/core";
import { Observable } from 'rxjs';
import { HubConnection, HubConnectionBuilder } from "@aspnet/signalr";
import { AppConfig } from "@config/config";
import { NgRedux } from "@angular-redux/store";
import { AppState } from "@store/app.state";
import { HubClient } from "./hub.client";

//Interface to the business layer
@Injectable()
export class MainService extends HubClient {
    private hubUrlChange = false;
    private hubConnection: HubConnection;
    private hubUrl = this.config.setting["HubUrl"];
    private hubName = this.config.setting["HubName"];

    constructor(private config: AppConfig, private ngRedux: NgRedux<AppState>) {
        super();
        let url: string = this.hubUrl +"/"+ this.hubName;
        super.setRedux(ngRedux);

        this.hubConnection = new HubConnectionBuilder()
        .withUrl(url)
        .build();
    }

    start(){
        this.hubConnection.onclose(function(){
            console.log("closed");
        });

        this.hubConnection.on("notifyCurrentPrices",super.notifyCurrentPrices);
        this.hubConnection.on("notifyInformation",super.notifyInformation);


        this.hubConnection.start()
        .then(function(){
            console.log("Started");
        })
        .catch((reason: any) =>{
            console.log(reason);
        })
    }
    
    initialiseSubscriptions(){

    }

    exception_handler(title:string, description: string, style:string){

    }

    set_Huburl(hubUrl:string){
        this.hubUrlChange  = true;
        this.hubUrl = hubUrl;
        //this.window.clearInterval(this.serverHubInitId)
    }
}
