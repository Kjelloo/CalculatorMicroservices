import {OperatorDto} from "./OperatorDto";

export interface CalculationRequest {
  operand1: number;
  operand2: number;
  operator: OperatorDto;
}
