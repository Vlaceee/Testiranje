import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthStorageService } from './auth-storage.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthStorageService).token();
  return next(token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request);
};

