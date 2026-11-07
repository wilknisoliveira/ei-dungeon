import { DOCUMENT } from '@angular/common';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { SITE_ORIGIN } from './site-url';
import { LandingSeoService } from './landing-seo.service';

describe('LandingSeoService', () => {
    let service: LandingSeoService;
    let document: Document;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                { provide: LOCALE_ID, useValue: 'pt-BR' },
                { provide: SITE_ORIGIN, useValue: 'https://ei-dungeon.example' },
            ],
        });

        service = TestBed.inject(LandingSeoService);
        document = TestBed.inject(DOCUMENT);
        document.head.innerHTML = '<meta name="robots" content="noindex, nofollow">';
    });

    afterEach(() => service.clear());

    it('creates one localized, indexable canonical and a complete alternate set', () => {
        service.apply();

        expect(TestBed.inject(Title).getTitle()).toBe(
            'EI-DUNGEON | Your AI Game Master',
        );
        expect(document.head.querySelector('meta[name="robots"]')?.getAttribute('content')).toBe(
            'index, follow',
        );
        expect(
            document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]')?.href,
        ).toBe('https://ei-dungeon.example/pt-BR/');

        const alternates = Array.from(
            document.head.querySelectorAll<HTMLLinkElement>('link[rel="alternate"]'),
        ).map((link) => [link.hreflang, link.href]);
        expect(alternates).toEqual([
            ['en', 'https://ei-dungeon.example/en/'],
            ['pt-BR', 'https://ei-dungeon.example/pt-BR/'],
            ['es', 'https://ei-dungeon.example/es/'],
            ['x-default', 'https://ei-dungeon.example/en/'],
        ]);
        expect(document.head.querySelectorAll('meta[name="description"]')).toHaveSize(1);
    });

    it('adds locale-aware social metadata and a deterministic JSON-LD graph', () => {
        service.apply();

        expect(
            document.head.querySelector('meta[property="og:image"]')?.getAttribute('content'),
        ).toBe('https://ei-dungeon.example/pt-BR/assets/logo_completed.webp');
        expect(
            document.head.querySelector('meta[property="og:locale"]')?.getAttribute('content'),
        ).toBe('pt_BR');
        expect(
            document.head.querySelector('meta[property="og:image:width"]')?.getAttribute('content'),
        ).toBe('1200');
        expect(
            document.head.querySelector('meta[property="twitter:card"]')?.getAttribute('content'),
        ).toBe('summary_large_image');

        const jsonLd = JSON.parse(
            document.head.querySelector('script[type="application/ld+json"]')?.textContent ?? '',
        );
        expect(jsonLd['@graph'].map((item: { ['@type']: string }) => item['@type'])).toEqual([
            'WebSite',
            'WebApplication',
        ]);
        expect(jsonLd['@graph'][1].applicationCategory).toBe('GameApplication');
    });

    it('replaces its own tags when applied more than once and restores noindex on cleanup', () => {
        service.apply();
        service.apply();

        expect(document.head.querySelectorAll('[data-landing-seo]')).toHaveSize(21);

        service.clear();

        expect(document.head.querySelectorAll('[data-landing-seo]')).toHaveSize(0);
        expect(document.head.querySelector('meta[name="robots"]')?.getAttribute('content')).toBe(
            'noindex, nofollow',
        );
    });
});
