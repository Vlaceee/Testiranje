import { createFeatureSelector, createSelector } from '@ngrx/store';
import { CartState } from './cart.reducer';

export const selectCartState = createFeatureSelector<CartState>('cart');
export const selectBackendCart = createSelector(selectCartState, state => state.backendCart);
export const selectAnonymousCartItems = createSelector(selectCartState, state => state.anonymousItems);
export const selectCartLoading = createSelector(selectCartState, state => state.loading);
export const selectCartError = createSelector(selectCartState, state => state.error);
export const selectAnonymousCount = createSelector(selectAnonymousCartItems, items => items.reduce((sum, item) => sum + item.quantity, 0));
export const selectBackendCount = createSelector(selectBackendCart, cart => cart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0);

