import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap } from 'rxjs';
import { apiErrorMessage } from '../../core/api-error';
import { ProductApiService } from '../../core/api.services';
import { ProductsActions } from './products.actions';

@Injectable()
export class ProductsEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(ProductApiService);

  readonly load$ = createEffect(() => this.actions$.pipe(
    ofType(ProductsActions.load),
    switchMap(({ query }) => this.api.search(query).pipe(
      map(result => ProductsActions.loadSuccess({ result })),
      catchError(error => of(ProductsActions.loadFailure({ error: apiErrorMessage(error) })))
    ))
  ));

  readonly categories$ = createEffect(() => this.actions$.pipe(
    ofType(ProductsActions.loadCategories),
    switchMap(() => this.api.categories().pipe(
      map(categories => ProductsActions.loadCategoriesSuccess({ categories })),
      catchError(error => of(ProductsActions.loadCategoriesFailure({ error: apiErrorMessage(error) })))
    ))
  ));
}

