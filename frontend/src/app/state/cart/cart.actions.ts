import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { AnonymousCartItem, Cart, Product } from '../../models/api.models';

export const CartActions = createActionGroup({
  source: 'Cart',
  events: {
    'Hydrate anonymous': props<{ items: AnonymousCartItem[] }>(),
    'Add anonymous': props<{ product: Product }>(),
    'Set anonymous quantity': props<{ productId: string; quantity: number }>(),
    'Remove anonymous': props<{ productId: string }>(),
    'Clear anonymous': emptyProps(),
    Load: emptyProps(),
    'Add authenticated': props<{ productId: string }>(),
    'Set authenticated quantity': props<{ productId: string; quantity: number }>(),
    'Remove authenticated': props<{ productId: string }>(),
    'Merge after login': emptyProps(),
    'Load success': props<{ cart: Cart }>(),
    'Operation failure': props<{ error: string }>()
  }
});

