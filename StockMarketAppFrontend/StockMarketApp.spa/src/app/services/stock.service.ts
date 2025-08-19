import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class StockService {

  private apiUrl = 'https://localhost:32770/api/Stocks/tracked-stocks';
  private apiKey = 'YOUR_API_KEY'; 

  constructor(private http: HttpClient) {}

  getStocks(): Observable<string[]> {
    const headers = new HttpHeaders().set("x-api-key", this.apiKey)
    return this.http.get<string[]>(this.apiUrl, { headers });
  }
}
