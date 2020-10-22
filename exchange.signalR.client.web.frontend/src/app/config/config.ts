import { Injectable } from '@angular/core';

@Injectable()
export class AppConfig {
    private _config: { [key: string]: string };
    constructor() {
        this._config = {
            HubUrl: 'http://192.168.1.203:5000/hubs',
            HubName: 'exchange'
        };
    }

    get setting(): { [key: string]: string } {
        return this._config;
    }

    get(key: any) {
        return this._config[key];
    }

    set(key: any, value: any){
        this._config.HubUrl = value;
    }
};