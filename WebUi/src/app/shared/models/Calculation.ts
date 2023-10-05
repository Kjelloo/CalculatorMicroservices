import {OperatorDto} from "./OperatorDto";

export interface Calculation {
  id: number;
  operand1: number;
  operand2: number;
  operator: OperatorDto;
  result: number;
}
