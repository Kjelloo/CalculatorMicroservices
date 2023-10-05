import { Component } from '@angular/core';
import {CalculatorService} from "../calculator.service";
import {CalculationRequest} from "../shared/models/CalculationRequest";
import {OperatorDto} from "../shared/models/OperatorDto";
import {CalculationHistoryComponent} from "../calculation-history/calculation-history.component";

@Component({
  selector: 'app-calculator',
  templateUrl: './calculator.component.html',
  styleUrls: ['./calculator.component.css']
})

export class CalculatorComponent {
  num1: number = 0;
  num2: number = 0;
  result: number | null = null;
  selectedOperator: OperatorDto = 0; // Default to Addition

  constructor(private calculatorService: CalculatorService, private calculationHistoryComponent: CalculationHistoryComponent) {}

  performOperation() {
    const request: CalculationRequest = {
      operand1: this.num1,
      operand2: this.num2,
      operator: this.selectedOperator,
    };

    this.calculatorService.calculate(request)
      .subscribe(data => {
        this.result = data;

        this.calculationHistoryComponent.refreshCalculations();
      });
  }
}
