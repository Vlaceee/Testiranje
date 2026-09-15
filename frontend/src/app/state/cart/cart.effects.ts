import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { catchError, map, of, switchMap, take, tap, withLatestFrom } from 'rxjs';
import { apiErrorMessage } from '../../core/api-error';
import { CartApiService } from '../../core/api.services';
import { AuthActions } from '../auth/auth.actions';
import { selectAnonymousCartItems, selectBackendCart } from './cart.selectors';
import { CartActions } from './cart.actions';

const ANONYMOUS_CART_KEY = 'forgemart.anonymous-cart.v1';

@Injectable()
export class CartEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(CartApiService);
  private readonly store = inject(Store);

  readonly load$ = createEffect(() => this.actions$.pipe(
    ofType(CartActions.load),
    switchMap(() => this.api.get().pipe(
      map(cart => CartActions.loadSuccess({ cart })),
      catchError(error => of(CartActions.operationFailure({ error: apiErrorMessage(error) })))
    ))
  ));

  readonly addAuthenticated$ = createEffect(() => this.actions$.pipe(
    ofType(CartActions.addAuthenticated),
    withLatestFrom(this.store.select(selectBackendCart)),
    switchMap(([{ productId }, cart]) => {
      const quantity = (cart?.items.find(item => item.productId === productId)?.quantity ?? 0) + 1;
      return this.api.set(productId, quantity).pipe(
        map(updated => CartActions.loadSuccess({ cart: updated })),
        catchError(error => of(CartActions.operationFailure({ error: apiErrorMessage(error) })))
      );
    })
  ));

  readonly setAuthenticated$ = createEffect(() => this.actions$.pipe(
    ofType(CartActions.setAuthenticatedQuantity),
    switchMap(({ productId, quantity }) => this.api.set(productId, quantity).pipe(
      map(cart => CartActions.loadSuccess({ cart })),
      catchError(error => of(CartActions.operationFailure({ error: apiErrorMessage(error) })))
    ))
  ));

  readonly removeAuthenticated$ = createEffect(() => this.actions$.pipe(
    ofType(CartActions.removeAuthenticated),
    switchMap(({ productId }) => this.api.remove(productId).pipe(
      map(cart => CartActions.loadSuccess({ cart })),
      catchError(error => of(CartActions.operationFailure({ error: apiErrorMessage(error) })))
    ))
  ));

  readonly mergeAfterLogin$ = createEffect(() => this.actions$.pipe(
    ofType(AuthActions.authSuccess),
    withLatestFrom(this.store.select(selectAnonymousCartItems)),
    switchMap(([_action, items]) => {
      const request = items.map(item => ({ productId: item.product.id, quantity: item.quantity }));
      return (request.length ? this.api.merge(request) : this.api.get()).pipe(
        switchMap(cart => of(CartActions.loadSuccess({ cart }), CartActions.clearAnonymous())),
        catchError(error => of(CartActions.operationFailure({ error: apiErrorMessage(error) })))
      );
    })
  ));

  readonly persistAnonymous$ = createEffect(() => this.actions$.pipe(
    ofType(CartActions.addAnonymous, CartActions.setAnonymousQuantity, CartActions.removeAnonymous, CartActions.clearAnonymous),
    switchMap(() => this.store.select(selectAnonymousCartItems).pipe(take(1))),
    tap(items => localStorage.setItem(ANONYMOUS_CART_KEY, JSON.stringify(items)))
  ), { dispatch: false });
}

export function readAnonymousCart(): import('../../models/api.models').AnonymousCartItem[] {
  try { return JSON.parse(localStorage.getItem(ANONYMOUS_CART_KEY) ?? '[]'); }
  catch { return []; }
}

