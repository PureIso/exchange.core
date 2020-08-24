import { NgModule } from "@angular/core";
import { NgRedux } from "@angular-redux/store";
import { AppComponent } from "./app.component";
import { AppState } from "@store/app.state";
import { store } from "@reducers/combined.reducers";
import { SharedModule } from "@modules/shared.modules";
import { AppRoutingModule } from "./app-routing.module";
import { MainModule } from "@main/main.module";
import { MainRoutingModule } from "@main/main-routing.module";
import { MainService } from "@services/main.service";
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';

@NgModule({
  imports: [MainModule, MainRoutingModule, AppRoutingModule, SharedModule, MatToolbarModule, MatButtonModule, MatIconModule,
    MatExpansionModule],
  declarations: [AppComponent],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule {
  constructor(ngRedux: NgRedux<AppState>, mainService: MainService) {
    //Initial state of our store
    ngRedux.provideStore(store);
    mainService.startAsync();
  }
}
