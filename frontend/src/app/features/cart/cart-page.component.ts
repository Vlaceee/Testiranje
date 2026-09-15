import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { selectAuthUser, selectIsAuthenticated } from '../../state/auth/auth.selectors';
import { CartActions } from '../../state/cart/cart.actions';
import { selectAnonymousCartItems, selectBackendCart, selectCartError, selectCartLoading } from '../../state/cart/cart.selectors';
import { OrdersActions } from '../../state/orders/orders.actions';
import { selectOrdersError, selectOrdersLoading } from '../../state/orders/orders.selectors';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatSelectModule],
  template: `
    <section class="page-shell">
      <span class="eyebrow">Your workshop run</span><h1 class="page-title">Shopping cart.</h1>
      <p class="muted">Anonymous carts stay in this browser. Sign in and ForgeMart merges them with your saved cart.</p>
      @if (itemCount() === 0) {
        <div class="empty-state fm-panel"><mat-icon>shopping_cart</mat-icon><h2>Your cart is ready for a job.</h2><p>Add tools and supplies from the catalog.</p><a mat-flat-button routerLink="/">Browse products</a></div>
      } @else {
        <div class="cart-layout">
          <div class="items fm-panel">
            @if (authenticated()) {
              @for (item of backendCart()?.items ?? []; track item.productId) {
                <article><img [src]="item.imageUrl" [alt]="item.productName" (error)="imageFallback($event)"><div class="item-copy"><strong>{{ item.productName }}</strong><small>{{ item.sku }} · {{ item.unitOfMeasure }}</small><span>{{ money(item.unitPrice) }} each</span></div><div class="quantity"><button type="button" (click)="setBackend(item.productId, item.quantity - 1)" [disabled]="item.quantity <= 1">−</button><span>{{ item.quantity }}</span><button type="button" (click)="setBackend(item.productId, item.quantity + 1)" [disabled]="item.quantity >= item.availableStock">+</button></div><strong>{{ money(item.lineTotal) }}</strong><button mat-icon-button type="button" (click)="removeBackend(item.productId)" aria-label="Remove item"><mat-icon>delete_outline</mat-icon></button></article>
              }
            } @else {
              @for (item of anonymousItems(); track item.product.id) {
                <article><img [src]="item.product.imageUrl" [alt]="item.product.name" (error)="imageFallback($event)"><div class="item-copy"><strong>{{ item.product.name }}</strong><small>{{ item.product.sku }} · {{ item.product.unitOfMeasure }}</small><span>{{ money(item.product.currentPrice) }} each</span></div><div class="quantity"><button type="button" (click)="setAnonymous(item.product.id, item.quantity - 1)" [disabled]="item.quantity <= 1">−</button><span>{{ item.quantity }}</span><button type="button" (click)="setAnonymous(item.product.id, item.quantity + 1)" [disabled]="item.quantity >= item.product.stockQuantity">+</button></div><strong>{{ money(item.product.currentPrice * item.quantity * (1 + item.product.vatRate / 100)) }}</strong><button mat-icon-button type="button" (click)="removeAnonymous(item.product.id)" aria-label="Remove item"><mat-icon>delete_outline</mat-icon></button></article>
              }
            }
            @if (cartError()) { <p class="form-error">{{ cartError() }}</p> }
          </div>
          <aside class="summary fm-panel">
            <h2>Order summary</h2><div><span>Subtotal</span><strong>{{ money(subtotal()) }}</strong></div><div><span>VAT</span><strong>{{ money(vat()) }}</strong></div><div class="total"><span>Total</span><strong>{{ money(total()) }}</strong></div>
            @if (!authenticated()) {
              <a mat-flat-button routerLink="/login">Sign in to checkout</a><p>Your local items will merge with your saved cart after login.</p>
            } @else {
              <form [formGroup]="checkoutForm" (ngSubmit)="checkout()">
                <mat-form-field appearance="outline"><mat-label>Contact name</mat-label><input matInput formControlName="contactName"></mat-form-field>
                <mat-form-field appearance="outline"><mat-label>Email</mat-label><input matInput type="email" formControlName="contactEmail"></mat-form-field>
                <mat-form-field appearance="outline"><mat-label>Address</mat-label><input matInput formControlName="shippingAddress"></mat-form-field>
                <div class="city"><mat-form-field appearance="outline"><mat-label>City</mat-label><input matInput formControlName="shippingCity"></mat-form-field><mat-form-field appearance="outline"><mat-label>Postal code</mat-label><input matInput formControlName="shippingPostalCode"></mat-form-field></div>
                <mat-form-field appearance="outline"><mat-label>Simulated payment</mat-label><mat-select formControlName="paymentMethod"><mat-option value="CashOnDelivery">Cash on delivery</mat-option><mat-option value="MockCard">Mock card</mat-option></mat-select></mat-form-field>
                @if (checkoutForm.controls.paymentMethod.value === 'MockCard') { <mat-form-field appearance="outline"><mat-label>Fake test card</mat-label><input matInput formControlName="mockCardNumber" placeholder="4111111111111111"><mat-hint>4111… succeeds; 4000…0002 declines</mat-hint></mat-form-field> }
                @if (orderError()) { <p class="form-error">{{ orderError() }}</p> }
                <button mat-flat-button type="submit" [disabled]="checkoutForm.invalid || orderLoading()">{{ orderLoading() ? 'Processing…' : 'Place simulated order' }}</button>
              </form>
            }
          </aside>
        </div>
      }
    </section>
  `,
  styles: [`
    .cart-layout { display: grid; grid-template-columns: minmax(0, 1.7fr) minmax(340px, .8fr); gap: 24px; margin-top: 30px; align-items: start; } .items { overflow: hidden; }
    article { display: grid; grid-template-columns: 92px 1fr auto auto auto; align-items: center; gap: 18px; padding: 18px; border-bottom: 1px solid var(--fm-line); } article:last-child { border: 0; } article img { width: 92px; height: 72px; object-fit: cover; border-radius: 9px; background: #1f2a31; } .item-copy strong, .item-copy small, .item-copy span { display: block; } .item-copy small { margin: 4px 0; color: var(--fm-steel); font-size: .72rem; } .item-copy span { color: var(--fm-orange-deep); font-size: .78rem; font-weight: 700; }
    .quantity { display: flex; align-items: center; border: 1px solid var(--fm-line); border-radius: 8px; overflow: hidden; } .quantity button { width: 32px; height: 34px; border: 0; background: #f0f3f4; cursor: pointer; } .quantity span { min-width: 34px; text-align: center; font-weight: 800; }
    .summary { position: sticky; top: 100px; padding: 26px; } .summary h2 { margin-top: 0; } .summary > div { display: flex; justify-content: space-between; padding: 9px 0; color: var(--fm-steel); } .summary .total { margin: 12px 0 20px; padding: 18px 0 0; border-top: 2px solid var(--fm-ink); color: var(--fm-ink); font-size: 1.25rem; } .summary > a, .summary form > button { width: 100%; min-height: 48px; color: white !important; background: var(--fm-orange) !important; } .summary > p { color: var(--fm-steel); font-size: .75rem; line-height: 1.5; } mat-form-field { width: 100%; } .city { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
    .form-error { padding: 10px; color: var(--fm-danger); background: #fff0ef; border-radius: 8px; font-size: .8rem; }
    .empty-state mat-icon { width: 54px; height: 54px; font-size: 54px; color: var(--fm-orange); } .empty-state a { color: white; background: var(--fm-orange); }
    @media (max-width: 900px) { .cart-layout { grid-template-columns: 1fr; } .summary { position: static; } }
    @media (max-width: 620px) { article { grid-template-columns: 70px 1fr auto; } article img { width: 70px; height: 62px; } article > strong { grid-column: 2; } .quantity { grid-column: 2; width: max-content; } article > button { grid-row: 1; grid-column: 3; } }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CartPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly store = inject(Store);
  readonly authenticated = this.store.selectSignal(selectIsAuthenticated);
  readonly user = this.store.selectSignal(selectAuthUser);
  readonly backendCart = this.store.selectSignal(selectBackendCart);
  readonly anonymousItems = this.store.selectSignal(selectAnonymousCartItems);
  readonly cartLoading = this.store.selectSignal(selectCartLoading);
  readonly cartError = this.store.selectSignal(selectCartError);
  readonly orderLoading = this.store.selectSignal(selectOrdersLoading);
  readonly orderError = this.store.selectSignal(selectOrdersError);
  readonly itemCount = computed(() => this.authenticated() ? this.backendCart()?.items.length ?? 0 : this.anonymousItems().length);
  readonly subtotal = computed(() => this.authenticated() ? this.backendCart()?.subtotal ?? 0 : this.anonymousItems().reduce((sum, item) => sum + item.product.currentPrice * item.quantity, 0));
  readonly vat = computed(() => this.authenticated() ? this.backendCart()?.vatTotal ?? 0 : this.anonymousItems().reduce((sum, item) => sum + item.product.currentPrice * item.quantity * item.product.vatRate / 100, 0));
  readonly total = computed(() => this.subtotal() + this.vat());
  readonly checkoutForm = this.fb.nonNullable.group({
    contactName: ['', [Validators.required, Validators.minLength(3)]], contactEmail: ['', [Validators.required, Validators.email]],
    shippingAddress: ['', [Validators.required, Validators.minLength(5)]], shippingCity: ['', [Validators.required, Validators.minLength(2)]],
    shippingPostalCode: ['', [Validators.required, Validators.minLength(3)]], paymentMethod: 'CashOnDelivery', mockCardNumber: ''
  });
  ngOnInit(): void { if (this.authenticated()) this.store.dispatch(CartActions.load()); const user = this.user(); if (user) this.checkoutForm.patchValue({ contactName: `${user.firstName} ${user.lastName}`, contactEmail: user.email }); }
  setBackend(productId: string, quantity: number): void { if (quantity > 0) this.store.dispatch(CartActions.setAuthenticatedQuantity({ productId, quantity })); }
  removeBackend(productId: string): void { this.store.dispatch(CartActions.removeAuthenticated({ productId })); }
  setAnonymous(productId: string, quantity: number): void { this.store.dispatch(CartActions.setAnonymousQuantity({ productId, quantity })); }
  removeAnonymous(productId: string): void { this.store.dispatch(CartActions.removeAnonymous({ productId })); }
  checkout(): void { if (this.checkoutForm.valid) this.store.dispatch(OrdersActions.checkout({ payload: this.checkoutForm.getRawValue() })); }
  money(value: number): string { return new Intl.NumberFormat('sr-RS', { style: 'currency', currency: 'RSD' }).format(value); }
  imageFallback(event: Event): void { (event.target as HTMLImageElement).src = '/assets/products/tool-placeholder.svg'; }
}

