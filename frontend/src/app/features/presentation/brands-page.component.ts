import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '../../core/seo.service';

@Component({
  standalone: true,
  imports: [RouterLink],
  templateUrl: './brands-page.component.html',
  styleUrl: './brands-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BrandsPageComponent implements OnInit {
  private readonly seo = inject(SeoService);
  readonly search = signal('');
  readonly brands = [
    { name: 'Bosch Professional', category: 'Električni i merni alati', accent: '#0875be' },
    { name: 'Makita', category: 'Aku i električni alati', accent: '#008a91' },
    { name: 'DeWalt', category: 'Profesionalni alati', accent: '#f3b400' },
    { name: 'Milwaukee', category: 'Aku sistemi i pribor', accent: '#d9272e' },
    { name: 'Stanley', category: 'Ručni alati i skladištenje', accent: '#e8b500' },
    { name: 'Metabo', category: 'Električni alati', accent: '#2f8e4d' },
    { name: 'Wera', category: 'Odvijači i ključevi', accent: '#45a82d' },
    { name: 'Knipex', category: 'Profesionalna klešta', accent: '#cf1f2b' },
    { name: 'Husqvarna', category: 'Bašta i šumarstvo', accent: '#ee7314' },
    { name: 'Fischer', category: 'Pričvrsna tehnika', accent: '#df1927' },
    { name: 'Sika', category: 'Građevinska hemija', accent: '#f2c800' },
    { name: 'Rothenberger', category: 'Vodovodni alati', accent: '#dc232c' },
    { name: 'Leica Geosystems', category: 'Profesionalno merenje', accent: '#e3242b' },
    { name: 'Petzl', category: 'Zaštita i rad na visini', accent: '#f3bd00' },
    { name: 'Festool', category: 'Precizna obrada drveta', accent: '#62a431' },
    { name: 'Irwin', category: 'Ručni alati i pribor', accent: '#1555a2' }
  ];
  readonly filteredBrands = computed(() => {
    const query = this.search().trim().toLocaleLowerCase('sr');
    return query ? this.brands.filter(brand => `${brand.name} ${brand.category}`.toLocaleLowerCase('sr').includes(query)) : this.brands;
  });

  ngOnInit(): void {
    this.seo.update({
      title: 'Brendovi profesionalnog alata | Bosch, Makita, DeWalt | ForgeMart',
      description: 'Pretražite vodeće proizvođače profesionalnog alata, merne opreme, pričvrsne tehnike i građevinskog materijala u ForgeMart ponudi.',
      path: '/brands',
      image: '/assets/editorial/workshop-atlas.webp',
      schema: { '@context': 'https://schema.org', '@type': 'CollectionPage', name: 'Brendovi profesionalnog alata', numberOfItems: this.brands.length }
    });
  }

  updateSearch(event: Event): void { this.search.set((event.target as HTMLInputElement).value); }
}
