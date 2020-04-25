import { NgModule } from "@angular/core";
import { NgRedux } from "@angular-redux/store";
import { AppComponent } from "./app.component";
import { AppState } from "@store/app.state";
import { store } from "@reducers/combined.reducers";
import { SharedModule } from "@modules/shared.modules";
import { AppRoutingModule } from "./app-routing.module";
import { MainModule } from "@main/main.module";
import { MainRoutingModule } from "@main/main-routing.module";

@NgModule({
  imports: [MainModule, MainRoutingModule, AppRoutingModule, SharedModule],
  declarations: [AppComponent],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule {
  constructor(ngRedux: NgRedux<AppState>) {
    //Initial state of our store
    ngRedux.provideStore(store);
  }
}
