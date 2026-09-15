import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

export interface SeoPage {
  title: string;
  description: string;
  path: string;
  image?: string;
  type?: 'website' | 'article';
  schema?: Record<string, unknown> | Record<string, unknown>[];
}

@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly document = inject(DOCUMENT);
  private readonly meta = inject(Meta);
  private readonly title = inject(Title);

  update(page: SeoPage): void {
    const origin = this.document.location?.origin || 'https://forgemart.rs';
    const canonicalUrl = new URL(page.path, `${origin}/`).toString();
    const imageUrl = new URL(page.image ?? '/assets/editorial/home-hero.webp', `${origin}/`).toString();

    this.title.setTitle(page.title);
    this.setName('description', page.description);
    this.setName('robots', 'index, follow, max-image-preview:large, max-snippet:-1, max-video-preview:-1');
    this.setProperty('og:locale', 'sr_RS');
    this.setProperty('og:type', page.type ?? 'website');
    this.setProperty('og:site_name', 'ForgeMart');
    this.setProperty('og:title', page.title);
    this.setProperty('og:description', page.description);
    this.setProperty('og:url', canonicalUrl);
    this.setProperty('og:image', imageUrl);
    this.setName('twitter:card', 'summary_large_image');
    this.setName('twitter:title', page.title);
    this.setName('twitter:description', page.description);
    this.setName('twitter:image', imageUrl);
    this.setCanonical(canonicalUrl);
    this.setSchema(page.schema);
  }

  private setName(name: string, content: string): void {
    this.meta.updateTag({ name, content });
  }

  private setProperty(property: string, content: string): void {
    this.meta.updateTag({ property, content });
  }

  private setCanonical(url: string): void {
    let link = this.document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!link) {
      link = this.document.createElement('link');
      link.rel = 'canonical';
      this.document.head.appendChild(link);
    }
    link.href = url;
  }

  private setSchema(schema?: SeoPage['schema']): void {
    this.document.getElementById('page-structured-data')?.remove();
    if (!schema) return;
    const script = this.document.createElement('script');
    script.id = 'page-structured-data';
    script.type = 'application/ld+json';
    script.text = JSON.stringify(schema);
    this.document.head.appendChild(script);
  }
}
