import { Injectable } from '@angular/core';

@Injectable()
export class AppConfig {
    public APP_ID = "";
    public LOG_LEVEL = "";
    public HUBURL = "";
    public HUBNAME = "";
    public HOST = "";

    constructor() {
        if (process.env != undefined) {
            this.APP_ID = process.env.APP_ID;
            this.LOG_LEVEL = process.env.LOG_LEVEL;
            this.HUBURL = process.env.HUBURL;
            this.HUBNAME = process.env.HUBNAME;
            this.HOST = process.env.HOST;
        } else {
            this.APP_ID = "N/A";
            this.LOG_LEVEL = "DEBUG";
            this.HUBURL = "http://localhost/backend/hubs";
            this.HUBNAME = "exchange";
            this.HOST = "0.0.0.0";
        }
    }
};