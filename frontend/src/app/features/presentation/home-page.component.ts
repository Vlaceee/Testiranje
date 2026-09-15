import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { SeoService } from '../../core/seo.service';
import { Product } from '../../models/api.models';
import { selectIsAuthenticated } from '../../state/auth/auth.selectors';
import { CartActions } from '../../state/cart/cart.actions';
import { selectAllProducts } from '../../state/products/products.selectors';
import { ShopPageComponent } from '../shop/shop-page.component';

type ProductTab = 'bestseller' | 'new' | 'professional' | 'sale';

@Component({
  selector: 'fm-home-page',
  standalone: true,
  imports: [RouterLink, ShopPageComponent],
  templateUrl: './home-page.component.html',
  styleUrl: './home-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomePageComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly seo = inject(SeoService);
  readonly products = this.store.selectSignal(selectAllProducts);
  readonly authenticated = this.store.selectSignal(selectIsAuthenticated);
  readonly selectedTab = signal<ProductTab>('bestseller');

  readonly categories = [
    { name: 'Električni alati', label: 'Snaga bez kompromisa', position: '46% 18%' },
    { name: 'Ručni alati', label: 'Preciznost u ruci', position: '78% 8%' },
    { name: 'Pričvrsni materijal', label: 'Šrafovi, ekseri i tiplovi', position: '6% 28%' },
    { name: 'Drvena građa', label: 'Za konstrukciju i detalje', position: '20% 48%' },
    { name: 'Građevinski materijal', label: 'Od temelja do završnice', position: '50% 58%' },
    { name: 'Zaštitna odeća', label: 'Sigurnost tokom celog dana', position: '70% 30%' },
    { name: 'Zaštitna obuća', label: 'Stabilan korak na terenu', position: '82% 32%' },
    { name: 'Bašta i dvorište', label: 'Uređen prostor napolju', position: '17% 10%' },
    { name: 'Elektro materijal', label: 'Pouzdane instalacije', position: '78% 82%' },
    { name: 'Vodovodni materijal', label: 'Spojevi koji traju', position: '47% 83%' },
    { name: 'Merna oprema', label: 'Dobar rezultat počinje merom', position: '54% 40%' },
    { name: 'Potrošni materijal', label: 'Uvek spremno pri ruci', position: '11% 78%' }
  ];

  readonly jobs = [
    { title: 'Renoviraš stan?', copy: 'Sve od pripreme i rušenja do čistih završnih radova.', position: '12% 52%', className: 'wide' },
    { title: 'Opremanje radionice', copy: 'Napravi prostor u kojem ništa ne nedostaje.', position: '52% 20%', className: '' },
    { title: 'Radovi u dvorištu', copy: 'Alat i materijal za sezonu napolju.', position: '16% 12%', className: '' },
    { title: 'Profesionalno gradilište', copy: 'Pouzdana oprema za svaki radni dan.', position: '78% 70%', className: '' },
    { title: 'DIY projekti', copy: 'Od ideje do stvari koju si napravio svojim rukama.', position: '32% 42%', className: 'wide' }
  ];

  readonly guides = [
    ['Aku bušilica', 'Kako izabrati aku bušilicu prema poslu koji radiš?'],
    ['Burgije', 'Koja burgija ide za beton, metal ili drvo?'],
    ['Zaštita', 'Kako izabrati zaštitne cipele za celodnevni rad?'],
    ['Obrada', 'Koju granulaciju šmirgle koristiti?'],
    ['SDS sistemi', 'Razlika između SDS, SDS Plus i SDS Max']
  ];

  readonly featuredProducts = computed(() => {
    const products = this.products();
    switch (this.selectedTab()) {
      case 'new': return [...products].reverse().slice(0, 4);
      case 'professional': return [...products].sort((a, b) => b.currentPrice - a.currentPrice).slice(0, 4);
      case 'sale': return products.filter(product => product.hasActiveDiscount).slice(0, 4);
      default: return products.slice(0, 4);
    }
  });
  readonly dealProduct = computed(() => this.products().find(product => product.hasActiveDiscount) ?? this.products()[0]);
  readonly newProducts = computed(() => [...this.products()].reverse().slice(0, 3));

  ngOnInit(): void {
    this.seo.update({
      title: 'ForgeMart | Profesionalni alat i građevinski materijal',
      description: 'Profesionalni alat, građevinski materijal, zaštitna oprema i pribor za radionicu, gradilište, dom i dvorište. Jasan izbor za svaki posao.',
      path: '/',
      image: '/assets/editorial/home-hero.webp',
      schema: [
        {
          '@context': 'https://schema.org', '@type': 'WebSite', name: 'ForgeMart',
          url: '/', potentialAction: { '@type': 'SearchAction', target: '/shop?search={search_term_string}', 'query-input': 'required name=search_term_string' }
        },
        {
          '@context': 'https://schema.org', '@type': 'HardwareStore', name: 'ForgeMart',
          description: 'Alat i građevinski materijal za radionicu, dom i gradilište.', image: '/assets/editorial/home-hero.webp', priceRange: 'RSD'
        }
      ]
    });
  }

  selectTab(tab: ProductTab): void { this.selectedTab.set(tab); }
  add(product: Product): void {
    this.store.dispatch(this.authenticated() ? CartActions.addAuthenticated({ productId: product.id }) : CartActions.addAnonymous({ product }));
  }
  money(value: number): string {
    return new Intl.NumberFormat('sr-RS', { style: 'currency', currency: 'RSD', maximumFractionDigits: 0 }).format(value);
  }
  imageFallback(event: Event): void { (event.target as HTMLImageElement).src = '/assets/products/tool-placeholder.svg'; }
}
