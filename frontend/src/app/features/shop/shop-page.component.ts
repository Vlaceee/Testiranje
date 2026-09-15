import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { SeoService } from '../../core/seo.service';
import { Product, ProductQuery } from '../../models/api.models';
import { selectIsAuthenticated } from '../../state/auth/auth.selectors';
import { CartActions } from '../../state/cart/cart.actions';
import { ProductsActions } from '../../state/products/products.actions';
import { selectAllProducts, selectProductCategories, selectProductPagination, selectProductsError, selectProductsLoading } from '../../state/products/products.selectors';

@Component({
  selector: 'fm-shop-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatCheckboxModule, MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatSelectModule],
  template: `
    <section class="page-shell catalog" id="catalog">
      <header class="catalog-heading"><div><span class="eyebrow">ForgeMart prodavnica</span><h2>Pronađi alat za sledeći posao.</h2></div><span class="result-count">{{ pagination().totalCount }} proizvoda</span></header>
      <form class="filters fm-panel" [formGroup]="filters" (ngSubmit)="applyFilters()">
        <mat-form-field appearance="outline" class="search"><mat-label>Search name, SKU, brand…</mat-label><input matInput formControlName="search"><mat-icon matPrefix>search</mat-icon></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Category</mat-label><mat-select formControlName="categoryId"><mat-option value="">All categories</mat-option>@for (category of categories(); track category.id) { <mat-option [value]="category.id">{{ category.name }}</mat-option> }</mat-select></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Sort</mat-label><mat-select formControlName="sort"><mat-option value="NameAsc">Name A-Z</mat-option><mat-option value="NameDesc">Name Z-A</mat-option><mat-option value="PriceAsc">Lowest price</mat-option><mat-option value="PriceDesc">Highest price</mat-option><mat-option value="Newest">Newest</mat-option><mat-option value="MostSold">Most sold</mat-option></mat-select></mat-form-field>
        <mat-checkbox formControlName="inStock">In stock</mat-checkbox>
        <mat-checkbox formControlName="discounted">On sale</mat-checkbox>
        <button mat-flat-button type="submit"><mat-icon>tune</mat-icon> Apply</button>
      </form>

      @if (loading()) {
        <div class="loading-state"><div class="loader"></div><p>Loading workshop inventory…</p></div>
      } @else if (error()) {
        <div class="error-state"><mat-icon>error_outline</mat-icon><p>{{ error() }}</p><button mat-button (click)="load()">Try again</button></div>
      } @else if (products().length === 0) {
        <div class="empty-state"><mat-icon>search_off</mat-icon><h3>No tools match those filters.</h3><p>Clear a filter and try the catalog again.</p></div>
      } @else {
        <div class="product-grid">
          @for (product of products(); track product.id) {
            <article class="product-card">
              <a class="image-wrap" [routerLink]="['/products', product.id]">
                @if (product.hasActiveDiscount) { <span class="discount">-{{ product.discountPercentage }}%</span> }
                <img [src]="product.imageUrl" [alt]="product.name" (error)="imageFallback($event)">
              </a>
              <div class="card-body">
                <div class="brand-row"><span>{{ product.brand }}</span><span [class.out]="product.stockQuantity <= 0" class="status-chip">{{ product.stockQuantity > 0 ? 'In stock' : 'Out of stock' }}</span></div>
                <a [routerLink]="['/products', product.id]"><h3>{{ product.name }}</h3></a>
                <p class="sku">{{ product.sku }} · per {{ unitLabel(product) }}</p>
                <div class="price-row"><div>@if (product.hasActiveDiscount) { <del>{{ money(product.price) }}</del> }<strong>{{ money(product.currentPrice) }}</strong><small>+ {{ product.vatRate }}% VAT</small></div>
                  <button mat-mini-fab type="button" aria-label="Add to cart" [disabled]="product.stockQuantity <= 0" (click)="add(product)"><mat-icon>add_shopping_cart</mat-icon></button>
                </div>
              </div>
            </article>
          }
        </div>
        <mat-paginator [length]="pagination().totalCount" [pageSize]="pagination().pageSize" [pageIndex]="pagination().page - 1" [pageSizeOptions]="[12, 20, 40]" (page)="pageChanged($event)" aria-label="Product pages" />
      }
    </section>
  `,
  styles: [`
    :host { display: block; background: #eef1f2; scroll-margin-top: 74px; }
    .catalog-heading { display: flex; align-items: end; justify-content: space-between; margin: 6px 0 22px; } .catalog-heading h2 { margin: 5px 0 0; font-size: clamp(2rem, 5vw, 3.4rem); letter-spacing: -.055em; } .result-count { color: var(--fm-steel); font-weight: 700; }
    .filters { display: grid; grid-template-columns: minmax(250px, 2fr) 1fr 1fr auto auto auto; align-items: center; gap: 12px; margin-bottom: 30px; padding: 16px 18px 0; box-shadow: none; } .filters mat-form-field { width: 100%; } .filters button { margin-bottom: 16px; color: white; background: var(--fm-ink); } .filters mat-checkbox { margin-bottom: 16px; }
    .product-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 20px; }
    .product-card { overflow: hidden; background: white; border: 1px solid var(--fm-line); border-radius: 15px; box-shadow: 0 10px 28px rgb(21 27 32 / 6%); transition: transform .2s ease, box-shadow .2s ease; } .product-card:hover { transform: translateY(-5px); box-shadow: var(--fm-shadow); }
    .image-wrap { position: relative; display: block; aspect-ratio: 1.42; overflow: hidden; background: #202c33; } .image-wrap img { width: 100%; height: 100%; object-fit: cover; transition: transform .3s ease; } .product-card:hover img { transform: scale(1.035); } .discount { position: absolute; z-index: 2; top: 12px; left: 12px; padding: 7px 10px; color: white; background: var(--fm-orange); border-radius: 5px; font-size: .72rem; font-weight: 900; }
    .card-body { padding: 18px; } .brand-row, .price-row { display: flex; align-items: center; justify-content: space-between; gap: 10px; } .brand-row > span:first-child { color: var(--fm-orange-deep); font-size: .7rem; font-weight: 900; letter-spacing: .1em; text-transform: uppercase; }
    h3 { min-height: 48px; margin: 14px 0 5px; font-size: 1.03rem; line-height: 1.35; } .sku { margin: 0 0 18px; color: var(--fm-steel); font-size: .73rem; } .price-row del, .price-row strong, .price-row small { display: block; } .price-row del { min-height: 16px; color: #8b979e; font-size: .72rem; } .price-row strong { font-size: 1.2rem; } .price-row small { color: var(--fm-steel); font-size: .65rem; } .price-row button { color: white; background: var(--fm-orange); }
    mat-paginator { margin-top: 28px; border-radius: 12px; background: transparent; } .loader { width: 36px; height: 36px; border: 4px solid #d6dde1; border-top-color: var(--fm-orange); border-radius: 50%; animation: spin .8s linear infinite; } @keyframes spin { to { transform: rotate(360deg); } }
    @media (max-width: 1050px) { .filters { grid-template-columns: 2fr 1fr 1fr; } .product-grid { grid-template-columns: repeat(3, 1fr); } }
    @media (max-width: 760px) { .filters { grid-template-columns: 1fr 1fr; } .search { grid-column: 1 / -1; } .product-grid { grid-template-columns: repeat(2, 1fr); } }
    @media (max-width: 480px) { .filters, .product-grid { grid-template-columns: 1fr; } .catalog-heading { align-items: start; flex-direction: column; gap: 8px; } }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShopPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly store = inject(Store);
  private readonly router = inject(Router);
  private readonly seo = inject(SeoService);
  readonly products = this.store.selectSignal(selectAllProducts);
  readonly categories = this.store.selectSignal(selectProductCategories);
  readonly pagination = this.store.selectSignal(selectProductPagination);
  readonly loading = this.store.selectSignal(selectProductsLoading);
  readonly error = this.store.selectSignal(selectProductsError);
  readonly authenticated = this.store.selectSignal(selectIsAuthenticated);
  readonly pageSize = signal(12);
  readonly filters = this.fb.nonNullable.group({ search: '', categoryId: '', sort: 'NameAsc' as ProductQuery['sort'], inStock: false, discounted: false });

  ngOnInit(): void {
    if (this.router.url.startsWith('/shop')) {
      this.seo.update({
        title: 'Prodavnica alata i građevinskog materijala | ForgeMart',
        description: 'Pretražite profesionalni električni i ručni alat, pričvrsni, elektro i vodovodni materijal, zaštitnu opremu i pribor za radionicu.',
        path: '/shop'
      });
    }
    this.store.dispatch(ProductsActions.loadCategories());
    this.load();
  }
  load(page = 1): void {
    const value = this.filters.getRawValue();
    this.store.dispatch(ProductsActions.load({ query: {
      page, pageSize: this.pageSize(), search: value.search || undefined, categoryId: value.categoryId || undefined,
      sort: value.sort, inStock: value.inStock || undefined, discounted: value.discounted || undefined
    } }));
  }
  applyFilters(): void { this.load(); }
  pageChanged(event: PageEvent): void { this.pageSize.set(event.pageSize); this.load(event.pageIndex + 1); }
  add(product: Product): void { this.store.dispatch(this.authenticated() ? CartActions.addAuthenticated({ productId: product.id }) : CartActions.addAnonymous({ product })); }
  money(value: number): string { return new Intl.NumberFormat('sr-RS', { style: 'currency', currency: 'RSD', maximumFractionDigits: 2 }).format(value); }
  unitLabel(product: Product): string { return product.unitOfMeasure === 'Pack' ? `pack of ${product.unitsPerPackage}` : product.unitOfMeasure.toLowerCase(); }
  imageFallback(event: Event): void { (event.target as HTMLImageElement).src = '/assets/products/tool-placeholder.svg'; }
}
