import { NgModule } from "@angular/core";
import { MainService } from "@services/main.service";
import { SharedModule } from "@modules/shared.modules";
import { MainRoutingModule } from "./main-routing.module";
import { AppConfig } from "@config/config";
import { DashboardComponent } from "./components/dashboard/dashboard.component";
import { PageNotFoundComponent } from "./components/page-not-found/page-not-found.component";
import { TradeComponent } from "./components/trade/trade.component";

@NgModule({
  imports: [MainRoutingModule, SharedModule],
  exports: [],
  declarations: [
    DashboardComponent,
    TradeComponent,
    PageNotFoundComponent
  ],
  providers: [MainService, AppConfig]
})

export class MainModule { }
