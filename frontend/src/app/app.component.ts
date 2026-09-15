import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Store } from '@ngrx/store';
import { AuthStorageService } from './core/auth-storage.service';
import { AuthActions } from './state/auth/auth.actions';
import { selectAuthUser, selectIsAdmin, selectIsAuthenticated } from './state/auth/auth.selectors';
import { CartActions } from './state/cart/cart.actions';
import { readAnonymousCart } from './state/cart/cart.effects';
import { selectAnonymousCount, selectBackendCount } from './state/cart/cart.selectors';

@Component({
  selector: 'fm-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatButtonModule, MatBadgeModule],
  template: `
    <header class="site-header">
      <a class="brand" routerLink="/" aria-label="ForgeMart home">
        <span class="brand-mark" aria-hidden="true">F</span>
        <span><strong>ForgeMart</strong><small>Alat i građevinski materijal</small></span>
      </a>
      <nav class="primary-nav" aria-label="Primary navigation">
        <a routerLink="/" [routerLinkActiveOptions]="{ exact: true }" routerLinkActive="active">Početna</a>
        <a routerLink="/shop" routerLinkActive="active">Prodavnica</a>
        <a routerLink="/brands" routerLinkActive="active">Brendovi</a>
        <a routerLink="/about" routerLinkActive="active">O nama</a>
        @if (authenticated()) { <a routerLink="/orders" routerLinkActive="active">Orders</a> }
        @if (isAdmin()) { <a routerLink="/admin" routerLinkActive="active">Admin</a> }
      </nav>
      <div class="header-actions">
        @if (user(); as currentUser) {
          <span class="user-chip"><span>{{ currentUser.firstName[0] }}{{ currentUser.lastName[0] }}</span>{{ currentUser.firstName }}</span>
          <button mat-button type="button" (click)="logout()">Logout</button>
        } @else {
          <a mat-button routerLink="/login">Login</a>
        }
        <a class="cart-link" mat-icon-button routerLink="/cart" aria-label="Shopping cart"
           [matBadge]="cartCount()" [matBadgeHidden]="cartCount() === 0" matBadgeColor="warn">
          <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 4h2l2.2 9.2a2 2 0 0 0 2 1.6h7.9a2 2 0 0 0 1.9-1.4L21 7H7M9 20a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3Zm8 0a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3Z"/></svg>
        </a>
      </div>
    </header>
    <main><router-outlet /></main>
    <footer class="site-footer">
      <div class="footer-main">
        <div class="footer-brand">
          <a class="footer-logo" routerLink="/"><span class="brand-mark" aria-hidden="true">F</span><strong>ForgeMart</strong></a>
          <p>Pravi alat. Jasan izbor.<br>Posao koji ide dalje.</p>
          <a href="mailto:podrska@forgemart.rs">podrska&#64;forgemart.rs</a>
        </div>
        <nav aria-label="Kupovina"><strong>Kupovina</strong><a routerLink="/shop">Sve kategorije</a><a routerLink="/shop">Novi proizvodi</a><a routerLink="/" fragment="popular">Akcije</a><a routerLink="/brands">Brendovi</a></nav>
        <nav aria-label="Pomoć"><strong>Pomoć</strong><a routerLink="/about">Dostava</a><a routerLink="/about">Plaćanje</a><a routerLink="/about">Povraćaj</a><a routerLink="/about">Garancija</a></nav>
        <nav aria-label="Za kupce"><strong>Za kupce</strong><a routerLink="/" fragment="projects">Saveti</a><a routerLink="/" fragment="projects">Projekti</a><a routerLink="/about">FAQ</a><a href="mailto:podrska@forgemart.rs">Kontakt</a></nav>
        <nav aria-label="Za firme"><strong>Za firme</strong><a routerLink="/about">B2B prodaja</a><a routerLink="/about">Veleprodaja</a><a href="mailto:prodaja@forgemart.rs">Zatraži ponudu</a></nav>
      </div>
      <div class="footer-bottom"><span>© 2026 ForgeMart. Studentski demonstracioni projekat — nema stvarnih plaćanja.</span><div><a routerLink="/about">Privatnost</a><a routerLink="/about">Uslovi korišćenja</a></div></div>
      <div class="footer-wordmark" aria-hidden="true">FORGEMART</div>
    </footer>
  `,
  styles: [`
    :host { display: block; min-height: 100vh; }
    .site-header { position: sticky; top: 0; z-index: 20; min-height: 74px; padding: 10px max(18px, calc((100vw - 1280px) / 2)); display: flex; align-items: center; gap: 34px; color: white; background: rgb(17 24 29 / 96%); border-bottom: 2px solid var(--fm-orange); backdrop-filter: blur(14px); }
    .brand { display: flex; align-items: center; gap: 11px; min-width: 250px; }
    .brand-mark { display: grid; place-items: center; width: 42px; height: 42px; border-radius: 12px 4px 12px 4px; color: white; background: var(--fm-orange); font-size: 1.15rem; font-weight: 950; transform: rotate(-2deg); }
    .brand strong, .brand small { display: block; } .brand strong { font-size: 1.17rem; letter-spacing: -.02em; } .brand small { margin-top: 1px; color: #aeb9c0; font-size: .66rem; text-transform: uppercase; letter-spacing: .08em; }
    .primary-nav { display: flex; align-self: stretch; align-items: center; gap: 22px; }
    .primary-nav a { position: relative; color: #cbd3d7; font-size: .9rem; font-weight: 700; }
    .primary-nav a.active, .primary-nav a:hover { color: white; }
    .primary-nav a.active::after { content: ''; position: absolute; left: 0; right: 0; bottom: -24px; height: 3px; background: var(--fm-orange); }
    .header-actions { margin-left: auto; display: flex; align-items: center; gap: 6px; }
    .user-chip { display: flex; align-items: center; gap: 8px; margin-right: 5px; color: #d9e0e3; font-size: .85rem; font-weight: 700; }
    .user-chip span { display: grid; place-items: center; width: 30px; height: 30px; border-radius: 50%; color: #1a2024; background: #f2a36e; font-size: .7rem; }
    .header-actions a, .header-actions button, .cart-link { color: white; }.cart-link svg { width: 23px; height: 23px; fill: none; stroke: currentColor; stroke-width: 1.8; stroke-linecap: round; stroke-linejoin: round; }
    main { min-height: calc(100vh - 190px); }
    .site-footer { position: relative; overflow: hidden; padding: 75px max(18px, calc((100vw - 1280px) / 2)) 18px; color: #9eabb2; background: #11181d; font-size: .78rem; }
    .footer-main { position: relative; z-index: 1; display: grid; grid-template-columns: 1.5fr repeat(4, 1fr); gap: 50px; padding-bottom: 64px; border-bottom: 1px solid rgb(255 255 255 / 11%); }
    .footer-brand p { margin: 24px 0; color: white; font-size: 1.35rem; font-weight: 750; line-height: 1.35; letter-spacing: -.03em; }.footer-brand>a:last-child { color: #ff8b49; }.footer-logo { display: flex; align-items: center; gap: 10px; color: white; font-size: 1.2rem; }
    .footer-main nav { display: flex; flex-direction: column; align-items: start; gap: 13px; }.footer-main nav strong { margin-bottom: 8px; color: white; font-size: .69rem; letter-spacing: .14em; text-transform: uppercase; }.footer-main nav a:hover { color: #ff8b49; }
    .footer-bottom { position: relative; z-index: 1; display: flex; justify-content: space-between; gap: 20px; padding-top: 22px; }.footer-bottom div { display: flex; gap: 22px; }
    .footer-wordmark { margin: 50px 0 -38px; color: rgb(255 255 255 / 3.5%); font-size: clamp(5rem, 16vw, 14rem); font-weight: 950; line-height: .72; letter-spacing: -.085em; text-align: center; white-space: nowrap; }
    @media (max-width: 1050px) { .primary-nav a:nth-child(3), .primary-nav a:nth-child(4) { display: none; }.footer-main { grid-template-columns: 1.5fr repeat(2, 1fr); }.footer-main nav:nth-last-child(-n+2) { margin-top: 15px; } }
    @media (max-width: 820px) { .site-header { gap: 10px; } .brand { min-width: 0; } .brand small, .user-chip, .primary-nav a:not(.active) { display: none; } .primary-nav { margin-left: auto; } .header-actions { margin-left: 0; }.footer-main { grid-template-columns: 1fr 1fr; }.footer-brand { grid-column: 1 / -1; }.footer-bottom { flex-direction: column; } }
    @media (max-width: 520px) { .brand strong { font-size: 1rem; } .brand-mark { width: 36px; height: 36px; } .primary-nav { display: none; }.site-footer { padding-top: 55px; }.footer-main { grid-template-columns: 1fr 1fr; gap: 38px 24px; }.footer-main nav:nth-last-child(-n+2) { margin-top: 0; }.footer-wordmark { margin-top: 35px; } }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly authStorage = inject(AuthStorageService);
  readonly user = this.store.selectSignal(selectAuthUser);
  readonly authenticated = this.store.selectSignal(selectIsAuthenticated);
  readonly isAdmin = this.store.selectSignal(selectIsAdmin);
  private readonly anonymousCount = this.store.selectSignal(selectAnonymousCount);
  private readonly backendCount = this.store.selectSignal(selectBackendCount);
  readonly cartCount = computed(() => this.authenticated() ? this.backendCount() : this.anonymousCount());

  ngOnInit(): void {
    const auth = this.authStorage.read();
    this.store.dispatch(AuthActions.hydrate({ auth }));
    this.store.dispatch(CartActions.hydrateAnonymous({ items: readAnonymousCart() }));
    if (auth) this.store.dispatch(CartActions.load());
  }

  // INTENTIONAL DEFECT: DEFECT-024
  // Educational purpose: logout clears authentication but leaves the previous backendCart in NgRx memory.
  // Expected failing tests: Logout_ShouldClearBackendCart and related account-switch isolation cases.
  // Correct production behavior: dispatch a cart-session reset together with logout.
  // Coursework baseline: DO NOT FIX until the intentional-defect exercise is completed.
  logout(): void { this.store.dispatch(AuthActions.logout()); }
}
