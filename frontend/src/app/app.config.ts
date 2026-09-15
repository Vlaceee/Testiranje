import { ApplicationConfig, isDevMode } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideStore } from '@ngrx/store';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth.interceptor';
import { AuthEffects } from './state/auth/auth.effects';
import { authReducer } from './state/auth/auth.reducer';
import { CartEffects } from './state/cart/cart.effects';
import { cartReducer } from './state/cart/cart.reducer';
import { OrdersEffects } from './state/orders/orders.effects';
import { ordersReducer } from './state/orders/orders.reducer';
import { ProductsEffects } from './state/products/products.effects';
import { productsReducer } from './state/products/products.reducer';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withComponentInputBinding(), withInMemoryScrolling({ scrollPositionRestoration: 'top', anchorScrolling: 'enabled' })),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimationsAsync(),
    provideStore({ auth: authReducer, products: productsReducer, cart: cartReducer, orders: ordersReducer }),
    provideEffects(AuthEffects, ProductsEffects, CartEffects, OrdersEffects),
    provideStoreDevtools({ maxAge: 25, logOnly: !isDevMode() })
  ]
};
