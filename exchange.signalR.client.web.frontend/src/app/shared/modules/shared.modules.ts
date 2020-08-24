import { BrowserModule } from "@angular/platform-browser";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { NgReduxModule } from "@angular-redux/store";
import { FormsModule } from "@angular/forms";
import { HttpClientModule } from "@angular/common/http";
import { NotificationComponent } from "./components/notification/notification.component";
import { PriceCardComponent } from "./components/price-card/price-card.component";
import { AccountInformationComponent } from "./components/account-information/account-information.component";
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from "@angular/material/core";
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatGridListModule } from '@angular/material/grid-list';

@NgModule({
    imports: [
        BrowserModule,
        HttpClientModule,
        CommonModule,
        FormsModule,
        NgReduxModule,
        MatExpansionModule,
        BrowserAnimationsModule,
        MatNativeDateModule ,
        MatToolbarModule, MatButtonModule, MatIconModule,MatFormFieldModule,
        MatInputModule,MatDatepickerModule,MatDividerModule,MatListModule,MatGridListModule
    ],
    exports: [
        FormsModule,
        CommonModule,
        BrowserAnimationsModule,
        NotificationComponent,
        PriceCardComponent,
        AccountInformationComponent,
        MatNativeDateModule ,
        MatToolbarModule, MatButtonModule, MatIconModule, MatExpansionModule, 
        MatFormFieldModule,MatInputModule,MatDatepickerModule,MatDividerModule,MatListModule,MatGridListModule
    ],
    providers: [MatDatepickerModule],
    declarations: [NotificationComponent, PriceCardComponent, AccountInformationComponent],
})
export class SharedModule {}
