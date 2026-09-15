import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { OrdersActions } from '../../state/orders/orders.actions';
import { selectOrders, selectOrdersError, selectOrdersLoading } from '../../state/orders/orders.selectors';

@Component({
  standalone: true,
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule],
  template: `<section class="page-shell"><span class="eyebrow">Purchase history</span><h1 class="page-title">Your orders.</h1><p class="muted">Price and product snapshots keep every historical order accurate.</p>
    @if (loading()) { <div class="loading-state">Loading orders…</div> } @else if (error()) { <div class="error-state">{{ error() }}</div> } @else if (orders().length === 0) { <div class="empty-state fm-panel"><mat-icon>receipt_long</mat-icon><h2>No orders yet.</h2><a mat-flat-button routerLink="/">Start shopping</a></div> } @else {
      <div class="order-list">@for (order of orders(); track order.id) { <a class="order fm-panel" [routerLink]="['/orders', order.id]"><div><small>{{ order.createdAt | date:'mediumDate' }}</small><strong>{{ order.orderNumber }}</strong></div><span class="status-chip">{{ order.status }}</span><div class="amount"><small>{{ order.items.length }} line items</small><strong>{{ money(order.grandTotal) }}</strong></div><mat-icon>chevron_right</mat-icon></a> }</div>
    }</section>`,
  styles: [`.order-list { display: grid; gap: 13px; margin-top: 28px; } .order { display: grid; grid-template-columns: 1fr auto 180px auto; align-items: center; gap: 22px; padding: 20px 24px; box-shadow: none; transition: border-color .2s, transform .2s; } .order:hover { border-color: var(--fm-orange); transform: translateX(4px); } .order strong, .order small { display: block; } .order small { color: var(--fm-steel); font-size: .72rem; } .amount { text-align: right; } .empty-state mat-icon { font-size: 48px; width: 48px; height: 48px; color: var(--fm-orange); } .empty-state a { color: white; background: var(--fm-orange); } @media(max-width:650px){.order{grid-template-columns:1fr auto}.amount{grid-column:1;text-align:left}.order>mat-icon{grid-column:2;grid-row:1}}`],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrdersPageComponent implements OnInit {
  private readonly store = inject(Store); readonly orders = this.store.selectSignal(selectOrders); readonly loading = this.store.selectSignal(selectOrdersLoading); readonly error = this.store.selectSignal(selectOrdersError);
  ngOnInit(): void { this.store.dispatch(OrdersActions.load()); }
  money(value: number): string { return new Intl.NumberFormat('sr-RS', { style: 'currency', currency: 'RSD' }).format(value); }
}
