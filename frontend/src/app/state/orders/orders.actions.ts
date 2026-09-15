import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { Order, PagedResult } from '../../models/api.models';

export const OrdersActions = createActionGroup({
  source: 'Orders',
  events: {
    Load: emptyProps(),
    'Load success': props<{ result: PagedResult<Order> }>(),
    'Load failure': props<{ error: string }>(),
    Checkout: props<{ payload: object }>(),
    'Checkout success': props<{ order: Order }>(),
    'Checkout failure': props<{ error: string }>()
  }
});

