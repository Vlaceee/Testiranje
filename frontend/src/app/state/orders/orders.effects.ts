import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap, tap } from 'rxjs';
import { apiErrorMessage } from '../../core/api-error';
import { OrderApiService } from '../../core/api.services';
import { CartActions } from '../cart/cart.actions';
import { OrdersActions } from './orders.actions';

@Injectable()
export class OrdersEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(OrderApiService);
  private readonly router = inject(Router);
  readonly load$ = createEffect(() => this.actions$.pipe(
    ofType(OrdersActions.load), switchMap(() => this.api.list().pipe(
      map(result => OrdersActions.loadSuccess({ result })),
      catchError(error => of(OrdersActions.loadFailure({ error: apiErrorMessage(error) })))
    ))
  ));
  readonly checkout$ = createEffect(() => this.actions$.pipe(
    ofType(OrdersActions.checkout), switchMap(({ payload }) => this.api.checkout(payload).pipe(
      map(order => OrdersActions.checkoutSuccess({ order })),
      catchError(error => of(OrdersActions.checkoutFailure({ error: apiErrorMessage(error) })))
    ))
  ));
  readonly checkoutSuccess$ = createEffect(() => this.actions$.pipe(
    ofType(OrdersActions.checkoutSuccess),
    tap(({ order }) => void this.router.navigate(['/orders', order.id])),
    map(() => CartActions.load())
  ));
}
