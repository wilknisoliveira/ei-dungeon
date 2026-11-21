import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { createAdminPaginatorIntl } from './admin-paginator-intl';

describe('AdminPaginatorIntl', () => {
    it('labels empty, zero-size, initial and subsequent pages', () => {
        const intl = TestBed.runInInjectionContext(() => createAdminPaginatorIntl());
        expect(intl.getRangeLabel(0, 10, 0)).toBe('0–0 of 0');
        expect(intl.getRangeLabel(0, 0, 40)).toBe('0–0 of 40');
        expect(intl.getRangeLabel(0, 10, 40)).toBe('1–10 of 40');
        expect(intl.getRangeLabel(3, 10, 35)).toBe('31–35 of 35');
    });

    for (const locale of ['en', 'pt-BR', 'es']) {
        it(`keeps English labels and number formatting under ${locale}`, () => {
            TestBed.configureTestingModule({ providers: [{ provide: LOCALE_ID, useValue: locale }] });
            const intl = TestBed.runInInjectionContext(() => createAdminPaginatorIntl());
            expect(intl.getRangeLabel(0, 10, 1000)).toBe('1–10 of 1,000');
            expect([intl.itemsPerPageLabel, intl.nextPageLabel, intl.previousPageLabel,
                intl.firstPageLabel, intl.lastPageLabel]).toEqual([
                'Items per page:', 'Next page', 'Previous page', 'First page', 'Last page',
            ]);
        });
    }
});
