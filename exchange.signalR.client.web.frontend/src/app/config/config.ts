import { Injectable } from '@angular/core';

@Injectable()
export class AppConfig {
    public APP_ID = "";
    public LOG_LEVEL = "";
    public HUBURL = "";
    public HUBNAME = "";

    constructor() {
        this.APP_ID = process.env.APP_ID;
        this.LOG_LEVEL = process.env.LOG_LEVEL;
        this.HUBURL = process.env.HUBURL;
        this.HUBNAME = process.env.HUBNAME;
    }
};