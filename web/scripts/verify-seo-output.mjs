import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const siteUrl = process.env.SITE_URL;
const locales = ['en', 'pt-BR', 'es'];
const outputRoot = resolve('dist/ei-dungeon-web/browser');

if (!siteUrl) {
    throw new Error('SITE_URL is required to verify generated SEO output.');
}

const origin = new URL(siteUrl).origin;
const fail = (message) => {
    throw new Error(`SEO output verification failed: ${message}`);
};

for (const locale of locales) {
    const html = readFileSync(resolve(outputRoot, locale, 'index.html'), 'utf8');
    const expectedUrl = `${origin}/${locale}/`;

    if (!/<h1[\s>][\s\S]*?\S[\s\S]*?<\/h1>/.test(html)) {
        fail(`${locale} does not contain the rendered landing H1.`);
    }
    if (!/name="robots" content="index, follow"/.test(html)) {
        fail(`${locale} is not indexable.`);
    }
    if ((html.match(/rel="canonical"/g) ?? []).length !== 1 || !html.includes(expectedUrl)) {
        fail(`${locale} does not have exactly one self-canonical URL.`);
    }
    for (const alternate of [...locales, 'x-default']) {
        if (!new RegExp(`hreflang="${alternate}"`).test(html)) {
            fail(`${locale} is missing its ${alternate} alternate.`);
        }
    }
    if (!html.includes('application/ld+json') || !html.includes('WebApplication')) {
        fail(`${locale} is missing JSON-LD.`);
    }
    if (!html.includes(`property="og:image" content="${expectedUrl}assets/logo_completed.webp"`)) {
        fail(`${locale} has no absolute Open Graph image.`);
    }
}

const robots = readFileSync(resolve(outputRoot, 'robots.txt'), 'utf8');
const sitemap = readFileSync(resolve(outputRoot, 'sitemap.xml'), 'utf8');

if (!robots.includes(`Sitemap: ${origin}/sitemap.xml`)) {
    fail('robots.txt does not advertise the generated sitemap.');
}
if ((sitemap.match(/<url>/g) ?? []).length !== locales.length) {
    fail('sitemap.xml must contain exactly the three landing URLs.');
}
if (/\/(?:login|signup|home|settings|game)(?:\/|<)/.test(sitemap)) {
    fail('sitemap.xml contains a non-landing URL.');
}
for (const locale of locales) {
    if (!sitemap.includes(`${origin}/${locale}/`)) {
        fail(`sitemap.xml is missing ${locale}.`);
    }
}

console.log('SEO output verification passed for en, pt-BR, and es.');
