import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { StockHistory } from '../../models/stockHistory';
import { Quote } from '../../models/Quote';
import { StockSearchResponse } from '../../models/StockSearchResponse';

@Injectable({
  providedIn: 'root'
})
export class StockService {

    private apiUrl = `${environment.apiUrl}/Stocks`;
  

  constructor(private http: HttpClient) {}

  getStocks(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/tracked-stocks`);
  }

  getSelectedStockHistory(stockSymbol: string): Observable<StockHistory> {
    return this.http.get<StockHistory>(`${this.apiUrl}/stock-history/${stockSymbol}`).pipe(
    map(history => ({
      ...history,
      createdOn: new Date(history.createdOn),
      lastUpdated: new Date(history.lastUpdated),
      quoteHistory: history.quoteHistory.map(q => ({
        ...q,
        date: new Date(q.date)   // if Quote has a Date too
      }))
    }))
    );
  }

  getCurrentQuote(stockSymbol: string): Observable<Quote> {
    return this.http.get<Quote>(`${this.apiUrl}/todays-quote/${stockSymbol}`);
  }

  searchStocks(query: string): Observable<StockSearchResponse> {
    return this.http.get<StockSearchResponse>(`${this.apiUrl}/search-stocks/${query}`);
  }

  addNewStock(symbol: string): Observable<string> {
    return this.http.post(
      `${this.apiUrl}/add-new-symbol/${symbol}`,
      undefined,
      { responseType: 'text' }
    );
  }

}
