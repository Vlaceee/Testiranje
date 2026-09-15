import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '../../core/seo.service';

@Component({
  standalone: true,
  imports: [RouterLink],
  templateUrl: './about-page.component.html',
  styleUrl: './about-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AboutPageComponent implements OnInit {
  private readonly seo = inject(SeoService);

  ngOnInit(): void {
    this.seo.update({
      title: 'O nama | ForgeMart — alat za ljude koji prave stvari',
      description: 'Upoznajte ForgeMart, naš pristup izboru profesionalnog alata, građevinskog materijala i podrške ljudima koji svakog dana nešto grade i popravljaju.',
      path: '/about',
      image: '/assets/editorial/professional-supply.webp',
      schema: { '@context': 'https://schema.org', '@type': 'AboutPage', name: 'O ForgeMart prodavnici', description: 'Alat za ljude koji prave stvari.' }
    });
  }
}
