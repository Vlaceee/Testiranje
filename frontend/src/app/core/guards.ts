import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { selectIsAdmin, selectIsAuthenticated } from '../state/auth/auth.selectors';

export const authGuard: CanActivateFn = () => {
  const store = inject(Store);
  return store.selectSignal(selectIsAuthenticated)() || inject(Router).createUrlTree(['/login']);
};

export const adminGuard: CanActivateFn = () => {
  const store = inject(Store);
  return store.selectSignal(selectIsAdmin)() || inject(Router).createUrlTree(['/']);
};
