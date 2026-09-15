import { Action } from '@ngrx/store';
import { AuthActions } from './state/auth/auth.actions';
import { CartActions } from './state/cart/cart.actions';
import { CartState, cartReducer } from './state/cart/cart.reducer';
import { courseworkBarHeight, courseworkPeriodStart } from './features/admin/admin-dashboard.component';
import { Cart, Product } from './models/api.models';

function product(id: string, stockQuantity: number): Product {
  return {
    id, name: 'Test tool', sku: `SKU-${id}`, brand: 'ForgePro', categoryId: 'category',
    categoryName: 'Tools', price: 100, currentPrice: 100, vatRate: 20, discountPercentage: 0,
    hasActiveDiscount: false, unitOfMeasure: 'Piece', unitsPerPackage: 1, stockQuantity,
    imageUrl: 'tool.svg', isActive: true
  };
}

function backendCart(): Cart {
  return { id: 'cart', items: [], distinctItemCount: 0, subtotal: 0, vatTotal: 0, grandTotal: 0, updatedAt: '2026-08-21T00:00:00Z' };
}

describe('Known intentional frontend defects - expected failures', () => {
  it('DEFECT-021 TodayPeriod_ShouldStartAtLocalMidnight', () => {
    const to = new Date(2026, 7, 21, 15, 30);
    expect(courseworkPeriodStart(to, 1)).toEqual(new Date(2026, 7, 21, 0, 0));
  });

  it('DEFECT-021 TodayPeriod_AtNoon_ShouldNotIncludePreviousNoon', () => {
    const to = new Date(2026, 7, 21, 12, 0);
    expect(courseworkPeriodStart(to, 1).getDate()).toBe(21);
  });

  it('DEFECT-022 FullRevenueBar_ShouldBeOneHundredPercent', () => expect(courseworkBarHeight(100, 100)).toBe(100));
  it('DEFECT-022 RevenueBar_ShouldNeverExceedOneHundredPercent', () => expect(courseworkBarHeight(200, 100)).toBeLessThanOrEqual(100));

  it('DEFECT-023 AnonymousCart_ShouldRejectZeroStock', () => {
    expect(cartReducer(undefined, CartActions.addAnonymous({ product: product('zero', 0) })).anonymousItems).toEqual([]);
  });
  it('DEFECT-023 AnonymousCart_ZeroStockShouldNotCreateQuantityOne', () => {
    expect(cartReducer(undefined, CartActions.addAnonymous({ product: product('none', 0) })).anonymousItems.length).toBe(0);
  });
  it('DEFECT-023 AnonymousCart_OutOfStockActionShouldLeaveStateUnchanged', () => {
    const initial = cartReducer(undefined, { type: '@@init' } as Action);
    expect(cartReducer(initial, CartActions.addAnonymous({ product: product('sold-out', 0) }))).toEqual(initial);
  });

  it('DEFECT-024 Logout_ShouldClearBackendCart', () => {
    const state: CartState = { backendCart: backendCart(), anonymousItems: [], loading: false, error: null };
    expect(cartReducer(state, AuthActions.logout()).backendCart).toBeNull();
  });
  it('DEFECT-024 Logout_ShouldClearPreviousCartError', () => {
    const state: CartState = { backendCart: backendCart(), anonymousItems: [], loading: false, error: 'Previous account error' };
    expect(cartReducer(state, AuthActions.logout()).error).toBeNull();
  });
  it('DEFECT-024 Logout_ShouldStopPreviousCartLoading', () => {
    const state: CartState = { backendCart: backendCart(), anonymousItems: [], loading: true, error: null };
    expect(cartReducer(state, AuthActions.logout()).loading).toBeFalse();
  });
});
