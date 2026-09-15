import { createReducer, on } from '@ngrx/store';
import { AnonymousCartItem, Cart } from '../../models/api.models';
import { CartActions } from './cart.actions';

export interface CartState {
  backendCart: Cart | null;
  anonymousItems: AnonymousCartItem[];
  loading: boolean;
  error: string | null;
}

const initialState: CartState = { backendCart: null, anonymousItems: [], loading: false, error: null };

export const cartReducer = createReducer(
  initialState,
  on(CartActions.hydrateAnonymous, (state, { items }) => ({ ...state, anonymousItems: items })),
  on(CartActions.addAnonymous, (state, { product }) => {
    const existing = state.anonymousItems.find(item => item.product.id === product.id);
    const items = existing
      ? state.anonymousItems.map(item => item.product.id === product.id
          ? { ...item, quantity: Math.min(item.quantity + 1, product.stockQuantity) }
          : item)
      // INTENTIONAL DEFECT: DEFECT-023
      // Educational purpose: direct action dispatch can add quantity 1 even when product stock is zero.
      // Expected failing tests: AnonymousCart_ShouldRejectZeroStock and related out-of-stock variants.
      // Correct production behavior: reject/ignore an out-of-stock add in the reducer or validated facade.
      // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
      : [...state.anonymousItems, { product, quantity: 1 }];
    return { ...state, anonymousItems: items, error: null };
  }),
  on(CartActions.setAnonymousQuantity, (state, { productId, quantity }) => ({
    ...state,
    anonymousItems: state.anonymousItems.map(item => item.product.id === productId
      ? { ...item, quantity: Math.max(1, Math.min(quantity, item.product.stockQuantity)) }
      : item)
  })),
  on(CartActions.removeAnonymous, (state, { productId }) => ({
    ...state, anonymousItems: state.anonymousItems.filter(item => item.product.id !== productId)
  })),
  on(CartActions.clearAnonymous, state => ({ ...state, anonymousItems: [] })),
  on(CartActions.load, CartActions.addAuthenticated, CartActions.setAuthenticatedQuantity,
    CartActions.removeAuthenticated, CartActions.mergeAfterLogin,
    state => ({ ...state, loading: true, error: null })),
  on(CartActions.loadSuccess, (state, { cart }) => ({ ...state, backendCart: cart, loading: false, error: null })),
  on(CartActions.operationFailure, (state, { error }) => ({ ...state, loading: false, error }))
);
