import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { CalculatorComponent } from './calculator/calculator.component';
import { HttpClientModule } from "@angular/common/http";
import { CalculatorService } from "./calculator.service";
import {FormsModule} from "@angular/forms";
import { CalculationHistoryComponent } from './calculation-history/calculation-history.component';
import {CalculatorHistoryService} from "./calculator-history.service";

@NgModule({
  declarations: [
    AppComponent,
    CalculatorComponent,
    CalculationHistoryComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    FormsModule
  ],
  providers: [CalculatorService, CalculatorHistoryService, CalculationHistoryComponent],
  bootstrap: [AppComponent]
})
export class AppModule { }
