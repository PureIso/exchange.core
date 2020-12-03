import { Routes, RouterModule } from "@angular/router";
import { NgModule } from "@angular/core";
import { DashboardComponent } from "./components/dashboard/dashboard.component";
import { TradeComponent } from "./components/trade/trade.component";

export const routes: Routes = [
    { path: 'dashboard', component: DashboardComponent },
    { path: 'trade', component: TradeComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
    declarations: []
})
export class MainRoutingModule { }
