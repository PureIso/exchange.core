import { Injectable } from '@angular/core';

@Injectable()
export class AppConfig {
    public APP_ID = "";
    public LOG_LEVEL = "";
    public HUBURL = "";
    public HUBNAME = "";
    public HOST = "";

    private _config: { [key: string]: string };

    constructor() {
        this._config = {
            PathAPI: 'http://localhost:5005/api/v1/'
        };

        if (process.env != undefined) {
            this.APP_ID = process.env.APP_ID;
            this.LOG_LEVEL = process.env.LOG_LEVEL;
            this.HUBURL = process.env.HUBURL;
            this.HUBNAME = process.env.HUBNAME;
            this.HOST = process.env.HOST;
        } else {
            this.APP_ID = "N/A";
            this.LOG_LEVEL = "DEBUG";
            this.HUBURL = "http://localhost:5000/hubs";
            this.HUBNAME = "exchange";
            this.HOST = "0.0.0.0";
        }
    }

    get setting(): { [key: string]: string } {
        return this._config;
    }

    get(key: any) {
        return this._config[key];
    }
};