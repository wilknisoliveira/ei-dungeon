import { DOCUMENT } from '@angular/common';
import { Injectable, LOCALE_ID, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { DEFAULT_LANGUAGE, SUPPORTED_LANGUAGES } from '../config/supported-languages';
import { SITE_ORIGIN } from './site-url';

const MANAGED_ATTRIBUTE = 'data-landing-seo';

@Injectable({ providedIn: 'root' })
export class LandingSeoService {
    private readonly document = inject(DOCUMENT);
    private readonly title = inject(Title);
    private readonly localeId = inject(LOCALE_ID);
    private readonly siteOrigin = inject(SITE_ORIGIN);

    apply(): void {
        this.removeManagedElements();

        const locale = this.resolveLocale();
        const canonicalUrl = this.localeUrl(locale);
        const title = $localize`:@@landingSeoTitle:EI-DUNGEON | Your AI Game Master`;
        const description = $localize`:@@landingSeoDescription:Play tabletop adventures with an AI Game Master that is ready whenever you are.`;

        this.title.setTitle(title);
        this.setRobots('index, follow');
        this.appendLink('canonical', canonicalUrl);

        for (const language of SUPPORTED_LANGUAGES) {
            this.appendLink('alternate', this.localeUrl(language.code), language.code);
        }
        this.appendLink('alternate', this.localeUrl(DEFAULT_LANGUAGE), 'x-default');

        this.appendMeta(
            'description',
            description,
        );

        const imageUrl = `${canonicalUrl}assets/logo_completed.webp`;
        this.appendProperty('og:title', title);
        this.appendProperty('og:description', description);
        this.appendProperty('og:type', 'website');
        this.appendProperty('og:url', canonicalUrl);
        this.appendProperty('og:locale', this.openGraphLocale(locale));
        this.appendProperty('og:image', imageUrl);
        this.appendProperty('og:image:width', '1200');
        this.appendProperty('og:image:height', '400');
        this.appendProperty(
            'og:image:alt',
            $localize`:@@landingSeoImageAlt:EI-DUNGEON logo over a fantasy dungeon landscape`,
        );
        this.appendProperty('twitter:card', 'summary_large_image');
        this.appendProperty('twitter:title', title);
        this.appendProperty('twitter:description', description);
        this.appendProperty('twitter:image', imageUrl);
        this.appendProperty(
            'twitter:image:alt',
            $localize`:@@landingSeoImageAlt:EI-DUNGEON logo over a fantasy dungeon landscape`,
        );
        this.appendJsonLd({
            '@context': 'https://schema.org',
            '@graph': [
                {
                    '@type': 'WebSite',
                    name: 'EI-DUNGEON',
                    url: canonicalUrl,
                    inLanguage: locale,
                },
                {
                    '@type': 'WebApplication',
                    name: 'EI-DUNGEON',
                    description,
                    url: canonicalUrl,
                    image: imageUrl,
                    applicationCategory: 'GameApplication',
                    genre: 'Role-playing game',
                    inLanguage: locale,
                },
            ],
        });
    }

    clear(): void {
        this.removeManagedElements();
        this.setRobots('noindex, nofollow');
    }

    private resolveLocale(): string {
        const normalized = this.localeId.toLowerCase();
        return (
            SUPPORTED_LANGUAGES.find(
                (language) =>
                    language.code.toLowerCase() === normalized ||
                    language.locale.toLowerCase() === normalized,
            )?.code ?? DEFAULT_LANGUAGE
        );
    }

    private localeUrl(locale: string): string {
        return `${this.siteOrigin}/${locale}/`;
    }

    private setRobots(content: string): void {
        let robots = this.document.head.querySelector<HTMLMetaElement>(
            'meta[name="robots"]',
        );

        if (!robots) {
            robots = this.document.createElement('meta');
            robots.name = 'robots';
            this.document.head.appendChild(robots);
        }

        robots.content = content;
    }

    private appendMeta(name: string, content: string): void {
        const meta = this.document.createElement('meta');
        meta.name = name;
        meta.content = content;
        meta.setAttribute(MANAGED_ATTRIBUTE, 'true');
        this.document.head.appendChild(meta);
    }

    private appendLink(rel: string, href: string, hreflang?: string): void {
        const link = this.document.createElement('link');
        link.rel = rel;
        link.href = href;
        if (hreflang) {
            link.hreflang = hreflang;
        }
        link.setAttribute(MANAGED_ATTRIBUTE, 'true');
        this.document.head.appendChild(link);
    }

    private appendProperty(property: string, content: string): void {
        const meta = this.document.createElement('meta');
        meta.setAttribute('property', property);
        meta.content = content;
        meta.setAttribute(MANAGED_ATTRIBUTE, 'true');
        this.document.head.appendChild(meta);
    }

    private appendJsonLd(data: object): void {
        const script = this.document.createElement('script');
        script.type = 'application/ld+json';
        script.textContent = JSON.stringify(data);
        script.setAttribute(MANAGED_ATTRIBUTE, 'true');
        this.document.head.appendChild(script);
    }

    private openGraphLocale(locale: string): string {
        return (
            SUPPORTED_LANGUAGES.find((language) => language.code === locale)?.locale.replace(
                '-',
                '_',
            ) ?? 'en_US'
        );
    }

    private removeManagedElements(): void {
        this.document.head
            .querySelectorAll(`[${MANAGED_ATTRIBUTE}]`)
            .forEach((element) => element.remove());
    }
}
