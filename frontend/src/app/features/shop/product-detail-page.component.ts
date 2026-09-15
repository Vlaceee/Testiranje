import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { catchError, map, of, switchMap } from 'rxjs';
import { ProductApiService } from '../../core/api.services';
import { Product } from '../../models/api.models';
import { selectIsAuthenticated } from '../../state/auth/auth.selectors';
import { CartActions } from '../../state/cart/cart.actions';

@Component({
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule],
  template: `
    <section class="page-shell">
      <a class="back" routerLink="/shop"><mat-icon>arrow_back</mat-icon> Back to catalog</a>
      @if (product(); as item) {
        <div class="product-layout fm-panel">
          <div class="visual"><img [src]="item.imageUrl" [alt]="item.name" (error)="imageFallback($event)">@if (item.hasActiveDiscount) { <span>-{{ item.discountPercentage }}%</span> }</div>
          <div class="details">
            <div class="brand">{{ item.brand }} · {{ item.sku }}</div>
            <h1>{{ item.name }}</h1>
            <p class="description">{{ item.description }}</p>
            <div class="availability"><span [class.out]="item.stockQuantity <= 0" class="status-chip">{{ item.stockQuantity > 0 ? item.stockQuantity + ' available' : 'Out of stock' }}</span><span>{{ item.categoryName }}</span></div>
            <div class="pricing">@if (item.hasActiveDiscount) { <del>{{ money(item.price) }}</del> }<strong>{{ money(item.currentPrice) }}</strong><small>Net price · VAT {{ item.vatRate }}% calculated at checkout</small></div>
            <dl><div><dt>Sold by</dt><dd>{{ unitLabel(item) }}</dd></div><div><dt>Package size</dt><dd>{{ item.unitsPerPackage }}</dd></div><div><dt>Low stock at</dt><dd>{{ item.minimumStockLevel }}</dd></div></dl>
            <button mat-flat-button class="add" [disabled]="item.stockQuantity <= 0" (click)="add(item)"><mat-icon>add_shopping_cart</mat-icon> Add to cart</button>
            <p class="simulated"><mat-icon>info</mat-icon> This is a simulated university store. No external payment is sent.</p>
          </div>
        </div>
      } @else if (error()) {
        <div class="error-state"><mat-icon>error_outline</mat-icon><p>{{ error() }}</p></div>
      } @else { <div class="loading-state">Loading product details…</div> }
    </section>
  `,
  styles: [`
    .back { display: inline-flex; align-items: center; gap: 7px; margin-bottom: 22px; color: var(--fm-steel); font-size: .86rem; font-weight: 700; }
    .product-layout { display: grid; grid-template-columns: 1.08fr .92fr; overflow: hidden; }
    .visual { position: relative; min-height: 560px; background: #1e2a31; } .visual img { width: 100%; height: 100%; object-fit: cover; } .visual > span { position: absolute; top: 24px; left: 24px; padding: 10px 13px; border-radius: 6px; color: white; background: var(--fm-orange); font-weight: 900; }
    .details { padding: clamp(30px, 6vw, 68px); } .brand { color: var(--fm-orange-deep); font-size: .78rem; font-weight: 900; letter-spacing: .1em; text-transform: uppercase; } h1 { margin: 12px 0 22px; font-size: clamp(2.5rem, 5vw, 4.5rem); line-height: .96; letter-spacing: -.06em; } .description { color: var(--fm-steel); line-height: 1.75; }
    .availability { display: flex; align-items: center; gap: 14px; margin: 23px 0; color: var(--fm-steel); font-size: .82rem; } .pricing { padding: 22px 0; border-block: 1px solid var(--fm-line); } .pricing del, .pricing strong, .pricing small { display: block; } .pricing del { color: #8b969d; } .pricing strong { margin: 2px 0; font-size: 2rem; } .pricing small { color: var(--fm-steel); }
    dl { display: grid; grid-template-columns: repeat(3, 1fr); margin: 24px 0; } dl div { padding-right: 12px; border-right: 1px solid var(--fm-line); } dl div:last-child { border: 0; } dt { color: var(--fm-steel); font-size: .7rem; text-transform: uppercase; } dd { margin: 4px 0 0; font-weight: 800; }
    .add { width: 100%; min-height: 50px; color: white !important; background: var(--fm-orange) !important; } .simulated { display: flex; align-items: center; gap: 8px; margin-top: 18px; color: var(--fm-steel); font-size: .74rem; } .simulated mat-icon { font-size: 18px; width: 18px; height: 18px; }
    @media (max-width: 800px) { .product-layout { grid-template-columns: 1fr; } .visual { min-height: 340px; } }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductDetailPageComponent {
  private readonly api = inject(ProductApiService);
  private readonly store = inject(Store);
  readonly authenticated = this.store.selectSignal(selectIsAuthenticated);
  readonly error = signal<string | null>(null);
  readonly product = toSignal(inject(ActivatedRoute).paramMap.pipe(
    map(params => params.get('id') ?? ''),
    switchMap(id => this.api.get(id)),
    catchError(() => { this.error.set('Product was not found or is no longer available.'); return of(undefined); })
  ));
  add(product: Product): void { this.store.dispatch(this.authenticated() ? CartActions.addAuthenticated({ productId: product.id }) : CartActions.addAnonymous({ product })); }
  money(value: number): string { return new Intl.NumberFormat('sr-RS', { style: 'currency', currency: 'RSD' }).format(value); }
  unitLabel(product: Product): string { return product.unitOfMeasure === 'Pack' ? `Pack of ${product.unitsPerPackage}` : product.unitOfMeasure; }
  imageFallback(event: Event): void { (event.target as HTMLImageElement).src = '/assets/products/tool-placeholder.svg'; }
}
