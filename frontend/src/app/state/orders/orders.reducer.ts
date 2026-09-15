import { createReducer, on } from '@ngrx/store';
import { Order } from '../../models/api.models';
import { OrdersActions } from './orders.actions';

export interface OrdersState { orders: Order[]; loading: boolean; error: string | null; lastOrder: Order | null; }
const initialState: OrdersState = { orders: [], loading: false, error: null, lastOrder: null };
export const ordersReducer = createReducer(
  initialState,
  on(OrdersActions.load, OrdersActions.checkout, state => ({ ...state, loading: true, error: null })),
  on(OrdersActions.loadSuccess, (state, { result }) => ({ ...state, orders: result.items, loading: false })),
  on(OrdersActions.checkoutSuccess, (state, { order }) => ({ ...state, lastOrder: order, orders: [order, ...state.orders], loading: false })),
  on(OrdersActions.loadFailure, OrdersActions.checkoutFailure, (state, { error }) => ({ ...state, loading: false, error }))
);

