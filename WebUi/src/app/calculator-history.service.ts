import { Injectable } from '@angular/core';
import {Calculation} from "./shared/models/Calculation";
import {Observable} from "rxjs";
import {HttpClient} from "@angular/common/http";

@Injectable({
  providedIn: 'root'
})
export class CalculatorHistoryService {
  private apiUrl = 'http://localhost:5080/api/CalculationHistory'; // Adjust the URL as needed

  constructor(private http: HttpClient) {}

  getCalculations(): Observable<Calculation[]> {
    return this.http.get<Calculation[]>(this.apiUrl);
  }
}
