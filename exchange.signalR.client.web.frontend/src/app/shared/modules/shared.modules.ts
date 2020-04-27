import { BrowserModule } from "@angular/platform-browser";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { NgReduxModule } from "@angular-redux/store";
import { FormsModule } from "@angular/forms";
import { HttpClientModule } from "@angular/common/http";
import { NotificationComponent } from "./components/notification/notification.component";
import { PriceCardComponent } from "./components/price-card/price-card.component";

@NgModule({
    imports: [
        BrowserModule,
        HttpClientModule,
        CommonModule,
        FormsModule,
        NgReduxModule,
        BrowserAnimationsModule,
    ],
    exports: [
        FormsModule,
        CommonModule,
        BrowserAnimationsModule,
        NotificationComponent,
        PriceCardComponent,
    ],
    providers: [],
    declarations: [NotificationComponent, PriceCardComponent],
})
export class SharedModule {}
