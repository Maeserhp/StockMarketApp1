import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class StockService {

    private apiUrl = `${environment.apiUrl}/Stocks`;
  

  constructor(private http: HttpClient) {}

  getStocks(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/tracked-stocks`);
  }
}
