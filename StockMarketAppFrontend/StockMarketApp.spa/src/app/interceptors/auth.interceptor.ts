import { HttpInterceptorFn } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth-service/auth.service';


export const AuthInterceptor: HttpInterceptorFn = (req, next) => {
  const authToken = inject(AuthService).getToken();

  if (authToken) {
    const authReq = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${authToken}`)
    });
    return next(authReq);
  }

  return next(req);
};