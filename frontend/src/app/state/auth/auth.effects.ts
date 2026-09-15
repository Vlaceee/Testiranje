import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap, tap } from 'rxjs';
import { apiErrorMessage } from '../../core/api-error';
import { AuthApiService } from '../../core/api.services';
import { AuthStorageService } from '../../core/auth-storage.service';
import { AuthActions } from './auth.actions';

@Injectable()
export class AuthEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(AuthApiService);
  private readonly storage = inject(AuthStorageService);
  private readonly router = inject(Router);

  readonly login$ = createEffect(() => this.actions$.pipe(
    ofType(AuthActions.login),
    switchMap(({ email, password }) => this.api.login(email, password).pipe(
      map(auth => AuthActions.authSuccess({ auth })),
      catchError(error => of(AuthActions.authFailure({ error: apiErrorMessage(error) })))
    ))
  ));

  readonly register$ = createEffect(() => this.actions$.pipe(
    ofType(AuthActions.register),
    switchMap(payload => this.api.register(payload).pipe(
      map(auth => AuthActions.authSuccess({ auth })),
      catchError(error => of(AuthActions.authFailure({ error: apiErrorMessage(error) })))
    ))
  ));

  readonly persist$ = createEffect(() => this.actions$.pipe(
    ofType(AuthActions.authSuccess),
    tap(({ auth }) => {
      this.storage.write(auth);
      void this.router.navigate(['/']);
    })
  ), { dispatch: false });

  readonly logout$ = createEffect(() => this.actions$.pipe(
    ofType(AuthActions.logout),
    tap(() => {
      this.storage.clear();
      void this.router.navigate(['/']);
    })
  ), { dispatch: false });
}

