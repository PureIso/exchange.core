import { BrowserModule } from "@angular/platform-browser";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { NgReduxModule } from "@angular-redux/store";
import { FormsModule } from "@angular/forms";
import { HttpClientModule } from "@angular/common/http";
import { NotificationComponent } from "./components/notification/notification.component";

@NgModule({
  imports: [
    BrowserModule,
    HttpClientModule,
    CommonModule,
    FormsModule,
    NgReduxModule,
    BrowserAnimationsModule
  ],
  exports: [FormsModule, CommonModule, BrowserAnimationsModule, NotificationComponent],
  providers: [],
  declarations: [NotificationComponent]
})
export class SharedModule { }
