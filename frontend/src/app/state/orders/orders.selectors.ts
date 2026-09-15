import { createFeatureSelector, createSelector } from '@ngrx/store';
import { OrdersState } from './orders.reducer';
export const selectOrdersState = createFeatureSelector<OrdersState>('orders');
export const selectOrders = createSelector(selectOrdersState, state => state.orders);
export const selectOrdersLoading = createSelector(selectOrdersState, state => state.loading);
export const selectOrdersError = createSelector(selectOrdersState, state => state.error);
export const selectLastOrder = createSelector(selectOrdersState, state => state.lastOrder);

