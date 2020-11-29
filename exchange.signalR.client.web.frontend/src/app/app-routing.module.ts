import { NgModule } from "@angular/core";
import { RouterModule, Routes } from "@angular/router";
import { DashboardComponent } from "@main/components/dashboard/dashboard.component";
import { PageNotFoundComponent } from "@main/components/page-not-found/page-not-found.component";

const appRoutes: Routes = [
  { path: "", component: DashboardComponent },
  { path: '**', component: PageNotFoundComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(appRoutes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
