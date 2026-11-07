import { normalizeSiteUrl } from './site-url';

describe('normalizeSiteUrl', () => {
    it('should normalize a production origin', () => {
        expect(normalizeSiteUrl('https://ei-dungeon.example/')).toBe(
            'https://ei-dungeon.example',
        );
    });

    it('should allow HTTP for local development', () => {
        expect(normalizeSiteUrl('http://localhost:4200')).toBe(
            'http://localhost:4200',
        );
    });

    it('should reject a missing value', () => {
        expect(() => normalizeSiteUrl('')).toThrowError(
            'SITE_URL is required for prerendering.',
        );
    });

    it('should reject a relative URL', () => {
        expect(() => normalizeSiteUrl('/en/')).toThrowError(
            'SITE_URL must be an absolute URL.',
        );
    });

    it('should reject HTTP outside local development', () => {
        expect(() => normalizeSiteUrl('http://ei-dungeon.example')).toThrowError(
            'SITE_URL must use HTTPS outside local development.',
        );
    });

    it('should reject a URL with a path', () => {
        expect(() =>
            normalizeSiteUrl('https://ei-dungeon.example/app'),
        ).toThrowError('SITE_URL must contain only the site origin.');
    });
});
