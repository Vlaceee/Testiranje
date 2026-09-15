import { Routes } from '@angular/router';
import { adminGuard, authGuard } from './core/guards';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/presentation/home-page.component').then(module => module.HomePageComponent),
    title: 'ForgeMart | Alat i građevinski materijal za svaki posao'
  },
  {
    path: 'shop',
    loadComponent: () => import('./features/shop/shop-page.component').then(module => module.ShopPageComponent),
    title: 'Prodavnica alata i materijala | ForgeMart'
  },
  {
    path: 'about',
    loadComponent: () => import('./features/presentation/about-page.component').then(module => module.AboutPageComponent),
    title: 'O nama | ForgeMart'
  },
  {
    path: 'brands',
    loadComponent: () => import('./features/presentation/brands-page.component').then(module => module.BrandsPageComponent),
    title: 'Brendovi alata | ForgeMart'
  },
  {
    path: 'products/:id',
    loadComponent: () => import('./features/shop/product-detail-page.component').then(module => module.ProductDetailPageComponent),
    title: 'Product | ForgeMart'
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart-page.component').then(module => module.CartPageComponent),
    title: 'Cart | ForgeMart'
  },
  {
    path: 'login',
    data: { mode: 'login' },
    loadComponent: () => import('./features/auth/auth-page.component').then(module => module.AuthPageComponent),
    title: 'Login | ForgeMart'
  },
  {
    path: 'register',
    data: { mode: 'register' },
    loadComponent: () => import('./features/auth/auth-page.component').then(module => module.AuthPageComponent),
    title: 'Register | ForgeMart'
  },
  {
    path: 'orders',
    canActivate: [authGuard],
    loadComponent: () => import('./features/orders/orders-page.component').then(module => module.OrdersPageComponent),
    title: 'Orders | ForgeMart'
  },
  {
    path: 'orders/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/orders/order-detail-page.component').then(module => module.OrderDetailPageComponent),
    title: 'Order | ForgeMart'
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () => import('./features/admin/admin-dashboard.component').then(module => module.AdminDashboardComponent),
    title: 'Admin Dashboard | ForgeMart'
  },
  {
    path: 'admin/:section',
    canActivate: [adminGuard],
    loadComponent: () => import('./features/admin/admin-section.component').then(module => module.AdminSectionComponent),
    title: 'Administration | ForgeMart'
  },
  { path: '**', redirectTo: '' }
];
