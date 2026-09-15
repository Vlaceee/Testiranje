import { SlicePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { catchError, of, switchMap } from 'rxjs';
import { AdminApiService } from '../../core/api.services';

// INTENTIONAL DEFECT: DEFECT-021
// Educational purpose: "Today" is implemented as a rolling 24-hour period instead of the local calendar day.
// Expected failing tests: TodayPeriod_ShouldStartAtLocalMidnight and TodayPeriod_AtNoon_ShouldNotIncludePreviousNoon.
// Correct production behavior: calculate the selected calendar-period boundary in the documented timezone.
// Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
export function courseworkPeriodStart(to: Date, days: number): Date {
  const from = new Date(to);
  from.setDate(to.getDate() - days);
  return from;
}

// INTENTIONAL DEFECT: DEFECT-022
// Educational purpose: chart height multiplies a percentage by 220 and can exceed the CSS 100% boundary.
// Expected failing tests: RevenueBar_ShouldNeverExceedOneHundredPercent and FullRevenueBar_ShouldBeOneHundredPercent.
// Correct production behavior: clamp a value/total percentage to the [2, 100] display range.
// Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
export function courseworkBarHeight(value: number, total: number): number {
  return total ? Math.max(2, value / total * 220) : 2;
}

@Component({
  standalone: true,
  imports: [SlicePipe, RouterLink, RouterLinkActive, MatButtonModule, MatIconModule],
  template: `
    <section class="admin-shell">
      <aside class="admin-nav"><div><span>FM</span><strong>Operations</strong></div><a routerLink="/admin" [routerLinkActiveOptions]="{exact:true}" routerLinkActive="active"><mat-icon>dashboard</mat-icon>Dashboard</a><a routerLink="/admin/products" routerLinkActive="active"><mat-icon>inventory_2</mat-icon>Products</a><a routerLink="/admin/categories" routerLinkActive="active"><mat-icon>category</mat-icon>Categories</a><a routerLink="/admin/inventory" routerLinkActive="active"><mat-icon>warehouse</mat-icon>Inventory</a><a routerLink="/admin/orders" routerLinkActive="active"><mat-icon>local_shipping</mat-icon>Orders</a><a routerLink="/admin/users" routerLinkActive="active"><mat-icon>group</mat-icon>Users</a><small>Coursework administration</small></aside>
      <main class="admin-content"><header><div><span class="eyebrow">Admin analytics</span><h1>Workshop pulse.</h1><p>Revenue, cost, orders, and stock risk in one clear view.</p></div><div class="periods"><button mat-button [class.active]="days()===1" (click)="days.set(1)">Today</button><button mat-button [class.active]="days()===7" (click)="days.set(7)">7 days</button><button mat-button [class.active]="days()===30" (click)="days.set(30)">30 days</button></div></header>
      @if (dashboard(); as data) {
        <div class="metrics"><article><small>Total revenue</small><strong>{{ money(data.totalRevenue) }}</strong><span>{{ data.numberOfOrders }} orders</span></article><article><small>Gross profit</small><strong>{{ money(data.grossProfit) }}</strong><span>{{ margin(data.totalRevenue,data.grossProfit) }}% margin</span></article><article><small>Average order</small><strong>{{ money(data.averageOrderValue) }}</strong><span>{{ data.productsSold }} units sold</span></article><article class="risk"><small>Low stock</small><strong>{{ data.lowStockProducts.length }}</strong><span>products need attention</span></article></div>
        <div class="dashboard-grid"><section class="fm-panel chart"><div><h2>Revenue trend</h2><span>{{ data.from | slice:0:10 }} - {{ data.to | slice:0:10 }}</span></div><div class="bars">@for(point of data.revenueSeries;track point.date){<div class="bar-column"><div class="bar" [style.height.%]="barHeight(point.revenue,data.totalRevenue)"><span>{{ money(point.revenue) }}</span></div><small>{{ point.date | slice:5 }}</small></div>}@empty{<p class="muted">No paid orders in this period.</p>}</div></section><section class="fm-panel low-stock"><div><h2>Low stock</h2><a routerLink="/admin/inventory">View inventory</a></div>@for(item of data.lowStockProducts.slice(0,6);track item.productId){<article><div><strong>{{ item.productName }}</strong><small>{{ item.sku }}</small></div><span [class.zero]="item.quantity===0">{{ item.quantity }} / {{ item.minimumStockLevel }}</span></article>}</section></div>
      } @else { <div class="loading-state">Loading dashboard…</div> }
      </main>
    </section>
  `,
  styles: [`
    .admin-shell{display:grid;grid-template-columns:240px 1fr;min-height:calc(100vh - 74px)}.admin-nav{display:flex;flex-direction:column;gap:7px;padding:28px 18px;color:#c7d0d5;background:#202a31}.admin-nav>div{display:flex;align-items:center;gap:10px;margin:0 8px 24px;color:white}.admin-nav>div span{display:grid;place-items:center;width:38px;height:38px;border-radius:8px;background:var(--fm-orange);font-weight:900}.admin-nav a{display:flex;align-items:center;gap:12px;padding:11px 13px;border-radius:8px;font-size:.85rem;font-weight:700}.admin-nav a:hover,.admin-nav a.active{color:white;background:#34434c}.admin-nav mat-icon{color:#91a0a8}.admin-nav a.active mat-icon{color:#ff8a3d}.admin-nav small{margin:auto 10px 0;color:#73838c}.admin-content{min-width:0;padding:38px clamp(22px,4vw,58px)}header{display:flex;justify-content:space-between;gap:20px;align-items:end}header h1{margin:5px 0;font-size:3rem;letter-spacing:-.055em}header p{margin:0;color:var(--fm-steel)}.periods{display:flex;padding:4px;border:1px solid var(--fm-line);border-radius:10px;background:white}.periods button.active{color:white;background:var(--fm-ink)}.metrics{display:grid;grid-template-columns:repeat(4,1fr);gap:16px;margin:30px 0}.metrics article{padding:21px;border-radius:13px;color:white;background:#26343d}.metrics small,.metrics strong,.metrics span{display:block}.metrics small{color:#aebac1;text-transform:uppercase;font-size:.68rem;letter-spacing:.08em}.metrics strong{margin:10px 0 5px;font-size:1.65rem}.metrics span{color:#aebac1;font-size:.72rem}.metrics .risk{background:#9e3d19}.dashboard-grid{display:grid;grid-template-columns:1.4fr .7fr;gap:18px}.chart,.low-stock{padding:24px;box-shadow:none}.chart>div:first-child,.low-stock>div:first-child{display:flex;justify-content:space-between;align-items:center}.chart h2,.low-stock h2{margin:0}.chart>div:first-child span,.low-stock a{color:var(--fm-steel);font-size:.75rem}.bars{height:260px;display:flex;align-items:end;gap:12px;padding-top:34px;border-bottom:1px solid var(--fm-line)}.bar-column{height:100%;flex:1;display:flex;flex-direction:column;justify-content:end;align-items:center;gap:8px}.bar{position:relative;width:min(42px,70%);min-height:3px;background:linear-gradient(var(--fm-orange),#bf4a0d);border-radius:5px 5px 0 0}.bar span{display:none;position:absolute;bottom:calc(100% + 5px);left:50%;transform:translateX(-50%);padding:4px;background:#222;color:white;font-size:.6rem;white-space:nowrap}.bar:hover span{display:block}.bar-column small{font-size:.62rem;color:var(--fm-steel)}.low-stock article{display:flex;justify-content:space-between;align-items:center;padding:14px 0;border-bottom:1px solid var(--fm-line)}.low-stock strong,.low-stock small{display:block}.low-stock small{color:var(--fm-steel);font-size:.7rem}.low-stock article>span{padding:6px 8px;border-radius:6px;color:#8a4a00;background:#fff2db;font-weight:800}.low-stock article>span.zero{color:var(--fm-danger);background:#feeceb}@media(max-width:1000px){.metrics{grid-template-columns:repeat(2,1fr)}.dashboard-grid{grid-template-columns:1fr}}@media(max-width:700px){.admin-shell{grid-template-columns:1fr}.admin-nav{display:none}.admin-content{padding:24px 14px}header{align-items:start;flex-direction:column}.metrics{grid-template-columns:1fr 1fr}}`],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminDashboardComponent {
  private readonly api=inject(AdminApiService); readonly days=signal(30); readonly dashboard=toSignal(toObservable(this.days).pipe(switchMap(days=>{const to=new Date();const from=courseworkPeriodStart(to,days);return this.api.dashboard(from.toISOString(),to.toISOString()).pipe(catchError(()=>of(undefined)));})));
  money(value:number):string{return new Intl.NumberFormat('sr-RS',{style:'currency',currency:'RSD',maximumFractionDigits:0}).format(value)}
  margin(revenue:number,profit:number):number{return revenue?Math.round(profit/revenue*100):0} barHeight(value:number,total:number):number{return courseworkBarHeight(value,total)}
}
