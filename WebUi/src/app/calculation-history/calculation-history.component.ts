import {Component, OnInit} from '@angular/core';
import {CalculatorHistoryService} from "../calculator-history.service";
import {Calculation} from "../shared/models/Calculation";

@Component({
  selector: 'app-calculation-history',
  templateUrl: './calculation-history.component.html',
  styleUrls: ['./calculation-history.component.css']
})
export class CalculationHistoryComponent implements OnInit{
  calculations: Calculation[] = [];

  constructor(private calculationService: CalculatorHistoryService) {}

  ngOnInit() {
    this.fetchCalculations();
  }

  fetchCalculations() {
    this.calculationService.getCalculations()
      .subscribe(calculations => {
        this.calculations = calculations.reverse();
      });
  }

  // Add this method to manually trigger fetching of calculations
  refreshCalculations() {
    this.fetchCalculations();
  }
}
