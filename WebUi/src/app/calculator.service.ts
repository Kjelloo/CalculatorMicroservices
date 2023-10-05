import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {CalculationRequest} from "./shared/models/CalculationRequest";

@Injectable({
  providedIn: 'root'
})
export class CalculatorService {

  private apiUrl = 'http://localhost:5180/api';

  constructor(private http: HttpClient) {}

  calculate(request: CalculationRequest): Observable<number> {
    return this.http.post<number>(`${this.apiUrl}/calculator`, request);
  }
}
