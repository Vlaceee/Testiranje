import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  AuthResponse, Cart, Category, Dashboard, InventoryItem, Order, PagedResult,
  Product, ProductDetail, ProductQuery
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);
  login(email: string, password: string) {
    return this.http.post<AuthResponse>('/api/v1/auth/login', { email, password });
  }
  register(payload: { email: string; password: string; firstName: string; lastName: string }) {
    return this.http.post<AuthResponse>('/api/v1/auth/register', payload);
  }
}

@Injectable({ providedIn: 'root' })
export class ProductApiService {
  private readonly http = inject(HttpClient);
  search(query: ProductQuery) {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') params = params.set(key, String(value));
    }
    return this.http.get<PagedResult<Product>>('/api/v1/products', { params });
  }
  get(id: string) { return this.http.get<ProductDetail>(`/api/v1/products/${id}`); }
  categories(includeInactive = false) {
    return this.http.get<Category[]>('/api/v1/categories', { params: { includeInactive } });
  }
}

@Injectable({ providedIn: 'root' })
export class CartApiService {
  private readonly http = inject(HttpClient);
  get() { return this.http.get<Cart>('/api/v1/cart'); }
  set(productId: string, quantity: number) {
    return this.http.put<Cart>(`/api/v1/cart/items/${productId}`, { quantity });
  }
  remove(productId: string) { return this.http.delete<Cart>(`/api/v1/cart/items/${productId}`); }
  merge(items: { productId: string; quantity: number }[]) {
    return this.http.post<Cart>('/api/v1/cart/merge', { items });
  }
}

@Injectable({ providedIn: 'root' })
export class OrderApiService {
  private readonly http = inject(HttpClient);
  list(page = 1, pageSize = 20) {
    return this.http.get<PagedResult<Order>>('/api/v1/orders', { params: { page, pageSize } });
  }
  get(id: string) { return this.http.get<Order>(`/api/v1/orders/${id}`); }
  checkout(payload: object) { return this.http.post<Order>('/api/v1/orders/checkout', payload); }
  cancel(id: string) { return this.http.post<Order>(`/api/v1/orders/${id}/cancel`, {}); }
}

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);
  dashboard(from?: string, to?: string) {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<Dashboard>('/api/v1/admin/dashboard', { params });
  }
  inventory(lowStockOnly = false) {
    return this.http.get<InventoryItem[]>('/api/v1/admin/inventory', { params: { lowStockOnly } });
  }
  adjustInventory(productId: string, payload: object) {
    return this.http.post<InventoryItem>(`/api/v1/admin/inventory/${productId}/adjust`, payload);
  }
  users() { return this.http.get<Array<Record<string, unknown>>>('/api/v1/admin/users'); }
  orders() { return this.http.get<PagedResult<Order>>('/api/v1/orders', { params: { page: 1, pageSize: 100 } }); }
  updateOrderStatus(id: string, status: string) {
    return this.http.put<Order>(`/api/v1/admin/orders/${id}/status`, { status });
  }
}

