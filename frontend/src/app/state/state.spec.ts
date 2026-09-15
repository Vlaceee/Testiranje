import { Action } from '@ngrx/store';
import { AuthResponse, Cart, Order, PagedResult, Product } from '../models/api.models';
import { AuthActions } from './auth/auth.actions';
import { authReducer } from './auth/auth.reducer';
import { selectAuthError, selectAuthLoading, selectAuthToken, selectAuthUser, selectIsAdmin, selectIsAuthenticated } from './auth/auth.selectors';
import { CartActions } from './cart/cart.actions';
import { cartReducer } from './cart/cart.reducer';
import { OrdersActions } from './orders/orders.actions';
import { ordersReducer } from './orders/orders.reducer';
import { ProductsActions } from './products/products.actions';
import { ProductsState, productsReducer } from './products/products.reducer';
import { selectProductPagination, selectProductsError, selectProductsLoading } from './products/products.selectors';

const init = { type: '@@init' } as Action;

function initialProductsState(): ProductsState {
  return productsReducer(undefined, init) as ProductsState;
}

function product(overrides: Partial<Product> = {}): Product {
  return {
    id: 'product-1', name: 'Cordless drill', sku: 'DRILL-001', brand: 'ForgePro',
    categoryId: 'category-1', categoryName: 'Power tools', price: 12000, currentPrice: 12000,
    vatRate: 20, discountPercentage: 0, hasActiveDiscount: false, unitOfMeasure: 'Piece',
    unitsPerPackage: 1, stockQuantity: 5, imageUrl: 'drill.svg', isActive: true, ...overrides
  };
}

function auth(role: 'Customer' | 'Admin' = 'Customer'): AuthResponse {
  return {
    accessToken: 'jwt-token', expiresAt: '2099-01-01T00:00:00Z',
    user: { id: 'user-1', email: 'customer@forgemart.test', firstName: 'Test', lastName: 'Customer', role, isActive: true }
  };
}

function cart(): Cart {
  return {
    id: 'cart-1', distinctItemCount: 1, subtotal: 1000, vatTotal: 200, grandTotal: 1200,
    updatedAt: '2026-08-21T00:00:00Z', items: [{
      id: 'item-1', productId: 'product-1', productName: 'Cordless drill', sku: 'DRILL-001',
      imageUrl: 'drill.svg', unitOfMeasure: 'Piece', quantity: 1, availableStock: 5,
      unitPrice: 1000, vatRate: 20, lineSubtotal: 1000, lineVat: 200, lineTotal: 1200,
      productIsActive: true
    }]
  };
}

function order(): Order {
  return {
    id: 'order-1', orderNumber: 'FM-000001', userId: 'user-1', customerName: 'Test Customer',
    status: 'PendingPayment', paymentStatus: 'Pending', paymentMethod: 'CashOnDelivery',
    createdAt: '2026-08-21T00:00:00Z', paidAt: null, shippedAt: null, deliveredAt: null,
    subtotal: 1000, vatTotal: 200, discountTotal: 0, grandTotal: 1200, costTotal: 700,
    contactName: 'Test Customer', contactEmail: 'customer@forgemart.test', shippingAddress: 'Main 1',
    shippingCity: 'Nis', shippingPostalCode: '18000', items: []
  };
}

describe('Auth NgRx state', () => {
  it('starts unauthenticated', () => expect(authReducer(undefined, init).token).toBeNull());
  it('sets loading on login', () => expect(authReducer(undefined, AuthActions.login({ email: 'a@b.c', password: 'secret' })).loading).toBeTrue());
  it('sets loading on registration', () => expect(authReducer(undefined, AuthActions.register({ email: 'a@b.c', password: 'secret', firstName: 'A', lastName: 'B' })).loading).toBeTrue());
  it('hydrates a stored session', () => expect(authReducer(undefined, AuthActions.hydrate({ auth: auth() })).token).toBe('jwt-token'));
  it('clears a session when hydration is empty', () => expect(authReducer(undefined, AuthActions.hydrate({ auth: null })).user).toBeNull());
  it('stores an authenticated admin', () => expect(authReducer(undefined, AuthActions.authSuccess({ auth: auth('Admin') })).user?.role).toBe('Admin'));
  it('records an authentication error', () => expect(authReducer(undefined, AuthActions.authFailure({ error: 'Invalid credentials' })).error).toBe('Invalid credentials'));
  it('clears state on logout', () => {
    const loggedIn = authReducer(undefined, AuthActions.authSuccess({ auth: auth() }));
    expect(authReducer(loggedIn, AuthActions.logout()).token).toBeNull();
  });

  it('selects the current token', () => expect(selectAuthToken.projector({ ...authReducer(undefined, init), token: 'abc' })).toBe('abc'));
  it('selects the current user', () => expect(selectAuthUser.projector({ ...authReducer(undefined, init), user: auth().user })).toEqual(auth().user));
  it('selects loading state', () => expect(selectAuthLoading.projector({ ...authReducer(undefined, init), loading: true })).toBeTrue());
  it('selects the current error', () => expect(selectAuthError.projector({ ...authReducer(undefined, init), error: 'failure' })).toBe('failure'));
  it('treats a token as authenticated', () => expect(selectIsAuthenticated.projector('abc')).toBeTrue());
  it('treats a missing token as anonymous', () => expect(selectIsAuthenticated.projector(null)).toBeFalse());
  it('recognizes an administrator', () => expect(selectIsAdmin.projector(auth('Admin').user)).toBeTrue());
  it('does not elevate a customer', () => expect(selectIsAdmin.projector(auth('Customer').user)).toBeFalse());
});

describe('Product NgRx state', () => {
  it('starts with an empty catalogue', () => expect(initialProductsState().ids).toEqual([]));
  it('sets loading while requesting products', () => expect((productsReducer(undefined, ProductsActions.load({ query: { page: 1, pageSize: 20 } })) as ProductsState).loading).toBeTrue());
  it('stores a paged result', () => {
    const result: PagedResult<Product> = { items: [product()], page: 2, pageSize: 10, totalCount: 21, totalPages: 3 };
    const state = productsReducer(undefined, ProductsActions.loadSuccess({ result })) as ProductsState;
    expect(state.entities['product-1']?.name).toBe('Cordless drill');
    expect(state.page).toBe(2);
    expect(state.totalPages).toBe(3);
  });
  it('sorts stored products by name', () => {
    const result: PagedResult<Product> = { items: [product({ id: 'b', name: 'Saw' }), product({ id: 'a', name: 'Drill' })], page: 1, pageSize: 20, totalCount: 2, totalPages: 1 };
    expect(productsReducer(undefined, ProductsActions.loadSuccess({ result })).ids).toEqual(['a', 'b']);
  });
  it('records a product loading error', () => expect((productsReducer(undefined, ProductsActions.loadFailure({ error: 'Offline' })) as ProductsState).error).toBe('Offline'));
  it('stores product categories', () => {
    const categories = [{ id: 'c1', name: 'Tools', description: 'Tools', isActive: true, productCount: 2 }];
    expect((productsReducer(undefined, ProductsActions.loadCategoriesSuccess({ categories })) as ProductsState).categories).toEqual(categories);
  });
  it('records a category loading error', () => expect((productsReducer(undefined, ProductsActions.loadCategoriesFailure({ error: 'Denied' })) as ProductsState).error).toBe('Denied'));
  it('selects pagination as a stable view model', () => expect(selectProductPagination.projector({ ...initialProductsState(), page: 3, pageSize: 5, totalCount: 12, totalPages: 3 })).toEqual({ page: 3, pageSize: 5, totalCount: 12, totalPages: 3 }));
  it('selects product loading', () => expect(selectProductsLoading.projector({ ...initialProductsState(), loading: true })).toBeTrue());
  it('selects product errors', () => expect(selectProductsError.projector({ ...initialProductsState(), error: 'Bad query' })).toBe('Bad query'));
});

describe('Anonymous cart NgRx state', () => {
  it('hydrates localStorage items', () => expect(cartReducer(undefined, CartActions.hydrateAnonymous({ items: [{ product: product(), quantity: 2 }] })).anonymousItems[0].quantity).toBe(2));
  it('adds a new product', () => expect(cartReducer(undefined, CartActions.addAnonymous({ product: product() })).anonymousItems.length).toBe(1));
  it('combines a duplicate product', () => {
    const once = cartReducer(undefined, CartActions.addAnonymous({ product: product() }));
    expect(cartReducer(once, CartActions.addAnonymous({ product: product() })).anonymousItems[0].quantity).toBe(2);
  });
  it('does not exceed available stock', () => {
    let state = cartReducer(undefined, CartActions.hydrateAnonymous({ items: [{ product: product({ stockQuantity: 2 }), quantity: 2 }] }));
    state = cartReducer(state, CartActions.addAnonymous({ product: product({ stockQuantity: 2 }) }));
    expect(state.anonymousItems[0].quantity).toBe(2);
  });
  it('clamps a quantity below one', () => {
    const state = cartReducer(undefined, CartActions.hydrateAnonymous({ items: [{ product: product(), quantity: 2 }] }));
    expect(cartReducer(state, CartActions.setAnonymousQuantity({ productId: 'product-1', quantity: 0 })).anonymousItems[0].quantity).toBe(1);
  });
  it('clamps a quantity above stock', () => {
    const state = cartReducer(undefined, CartActions.hydrateAnonymous({ items: [{ product: product({ stockQuantity: 5 }), quantity: 2 }] }));
    expect(cartReducer(state, CartActions.setAnonymousQuantity({ productId: 'product-1', quantity: 99 })).anonymousItems[0].quantity).toBe(5);
  });
  it('removes one product', () => {
    const state = cartReducer(undefined, CartActions.hydrateAnonymous({ items: [{ product: product(), quantity: 2 }] }));
    expect(cartReducer(state, CartActions.removeAnonymous({ productId: 'product-1' })).anonymousItems).toEqual([]);
  });
  it('clears the anonymous cart', () => {
    const state = cartReducer(undefined, CartActions.hydrateAnonymous({ items: [{ product: product(), quantity: 2 }] }));
    expect(cartReducer(state, CartActions.clearAnonymous()).anonymousItems).toEqual([]);
  });
  it('sets loading for backend operations', () => expect(cartReducer(undefined, CartActions.addAuthenticated({ productId: 'product-1' })).loading).toBeTrue());
  it('stores the authoritative backend cart', () => expect(cartReducer(undefined, CartActions.loadSuccess({ cart: cart() })).backendCart?.id).toBe('cart-1'));
  it('records a backend cart error', () => expect(cartReducer(undefined, CartActions.operationFailure({ error: 'Out of stock' })).error).toBe('Out of stock'));
});

describe('Order NgRx state', () => {
  it('starts without orders', () => expect(ordersReducer(undefined, init).orders).toEqual([]));
  it('sets loading while fetching history', () => expect(ordersReducer(undefined, OrdersActions.load()).loading).toBeTrue());
  it('sets loading during checkout', () => expect(ordersReducer(undefined, OrdersActions.checkout({ payload: {} })).loading).toBeTrue());
  it('stores paged order history', () => {
    const result: PagedResult<Order> = { items: [order()], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 };
    expect(ordersReducer(undefined, OrdersActions.loadSuccess({ result })).orders[0].orderNumber).toBe('FM-000001');
  });
  it('prepends a successful checkout', () => {
    const previous = { ...ordersReducer(undefined, init), orders: [{ ...order(), id: 'old' }] };
    const state = ordersReducer(previous, OrdersActions.checkoutSuccess({ order: order() }));
    expect(state.orders.map(item => item.id)).toEqual(['order-1', 'old']);
    expect(state.lastOrder?.id).toBe('order-1');
  });
  it('records an order loading error', () => expect(ordersReducer(undefined, OrdersActions.loadFailure({ error: 'Unavailable' })).error).toBe('Unavailable'));
  it('records a checkout error', () => expect(ordersReducer(undefined, OrdersActions.checkoutFailure({ error: 'Payment declined' })).error).toBe('Payment declined'));
});
