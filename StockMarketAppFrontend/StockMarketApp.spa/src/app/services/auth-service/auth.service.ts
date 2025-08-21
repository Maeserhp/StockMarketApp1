import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private apiUrl = `${environment.apiUrl}/Auth`;

  constructor(private http: HttpClient) { }

  login(username: string, password: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, { username, password });
  }

  // Store the JWT in local storage
  storeToken(token: string): void {
    localStorage.setItem('auth_token', token);
  }

  // Retrieve the token
  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  // Remove the token on logout
  logout(): void {
    localStorage.removeItem('auth_token');
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }
}
