import { Routes, RouterModule } from "@angular/router";
import { NgModule } from "@angular/core";
import { DashboardComponent } from "./components/dashboard/dashboard.component";
import { TradeComponent } from "./components/trade/trade.component";
import { MachineLearningComponent } from "./components/machinelearning/machinelearning.component";

export const routes: Routes = [
    { path: 'dashboard', component: DashboardComponent },
    { path: 'trade', component: TradeComponent },
    { path: 'machinelearning', component: MachineLearningComponent }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
    declarations: []
})
export class MainRoutingModule { }
