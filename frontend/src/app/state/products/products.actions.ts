import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { Category, PagedResult, Product, ProductQuery } from '../../models/api.models';

export const ProductsActions = createActionGroup({
  source: 'Products',
  events: {
    Load: props<{ query: ProductQuery }>(),
    'Load success': props<{ result: PagedResult<Product> }>(),
    'Load failure': props<{ error: string }>(),
    'Load categories': emptyProps(),
    'Load categories success': props<{ categories: Category[] }>(),
    'Load categories failure': props<{ error: string }>()
  }
});

