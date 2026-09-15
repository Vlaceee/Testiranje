import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, map, of, switchMap } from 'rxjs';
import { OrderApiService } from '../../core/api.services';
import { Order } from '../../models/api.models';

@Component({
  standalone: true,
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule],
  template: `<section class="page-shell"><a class="back" routerLink="/orders"><mat-icon>arrow_back</mat-icon> All orders</a>@if(order(); as item){<header><div><span class="eyebrow">{{ item.orderNumber }}</span><h1 class="page-title">Order details.</h1><p class="muted">Placed {{ item.createdAt | date:'medium' }}</p></div><span class="status-chip">{{ item.status }}</span></header><div class="layout"><div class="fm-panel lines"><h2>Items</h2>@for(line of item.items;track line.productId){<article><div><strong>{{ line.productName }}</strong><small>{{ line.sku }} · {{ line.quantity }} {{ line.unitOfMeasure }}</small></div><div><small>{{ money(line.unitPrice) }} + {{ line.vatRate }}% VAT</small><strong>{{ money(line.lineTotal) }}</strong></div></article>}<div class="totals"><span>Subtotal <strong>{{ money(item.subtotal) }}</strong></span><span>VAT <strong>{{ money(item.vatTotal) }}</strong></span><span class="grand">Total <strong>{{ money(item.grandTotal) }}</strong></span></div></div><aside class="fm-panel"><h2>Delivery</h2><strong>{{ item.contactName }}</strong><p>{{ item.shippingAddress }}<br>{{ item.shippingPostalCode }} {{ item.shippingCity }}</p><p>{{ item.contactEmail }}</p><hr><h2>Payment</h2><p>{{ item.paymentMethod }} · {{ item.paymentStatus }}</p><p class="note">Payment is simulated and never leaves ForgeMart.</p>@if(item.status==='PendingPayment'&&item.paymentStatus!=='Paid'){<button mat-stroked-button (click)="cancel(item.id)">Cancel unpaid order</button>}</aside></div>}@else{<div class="loading-state">Loading order…</div>}</section>`,
  styles: [`header{display:flex;justify-content:space-between;align-items:end;margin:16px 0 28px}.back{display:inline-flex;align-items:center;gap:6px;color:var(--fm-steel)}.layout{display:grid;grid-template-columns:1.5fr .7fr;gap:22px;align-items:start}.lines,aside{padding:26px}.lines h2,aside h2{margin-top:0}article{display:flex;justify-content:space-between;gap:20px;padding:18px 0;border-bottom:1px solid var(--fm-line)}article div:last-child{text-align:right}article strong,article small{display:block}article small,.note,aside p{color:var(--fm-steel);font-size:.8rem;line-height:1.6}.totals{margin-left:auto;width:min(100%,320px);padding-top:18px}.totals span{display:flex;justify-content:space-between;padding:6px}.totals .grand{margin-top:8px;padding-top:13px;border-top:2px solid;font-size:1.15rem}aside hr{border:0;border-top:1px solid var(--fm-line);margin:22px 0}aside button{width:100%;color:var(--fm-danger)}@media(max-width:760px){.layout{grid-template-columns:1fr}}`],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrderDetailPageComponent {
  private readonly api = inject(OrderApiService); private readonly destroyRef = inject(DestroyRef); readonly order = signal<Order | undefined>(undefined);
  constructor() { inject(ActivatedRoute).paramMap.pipe(map(p=>p.get('id')??''),switchMap(id=>this.api.get(id)),catchError(()=>of(undefined)),takeUntilDestroyed()).subscribe(order=>this.order.set(order)); }
  cancel(id:string):void{this.api.cancel(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(order=>this.order.set(order));}
  money(value:number):string{return new Intl.NumberFormat('sr-RS',{style:'currency',currency:'RSD'}).format(value);}
}
