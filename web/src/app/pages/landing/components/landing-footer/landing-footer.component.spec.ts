import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';

import { LandingFooterComponent } from './landing-footer.component';

describe('LandingFooterComponent', () => {
    let component: LandingFooterComponent;
    let fixture: ComponentFixture<LandingFooterComponent>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [LandingFooterComponent, RouterTestingModule],
        });
        fixture = TestBed.createComponent(LandingFooterComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should show the current year', () => {
        const host: HTMLElement = fixture.nativeElement;
        expect(host.querySelector('.copyright')?.textContent).toContain(
            String(new Date().getFullYear()),
        );
    });

    it('should link to signup and login', () => {
        const host: HTMLElement = fixture.nativeElement;
        const hrefs = Array.from(
            host.querySelectorAll<HTMLAnchorElement>('.footer-links a'),
        ).map((anchor) => anchor.getAttribute('href'));

        expect(hrefs).toContain('/signup');
        expect(hrefs).toContain('/login');
    });

    it('should expose crawlable links to every landing locale', () => {
        const host: HTMLElement = fixture.nativeElement;
        const localeLinks = Array.from(
            host.querySelectorAll<HTMLAnchorElement>('.language-links a'),
        ).map((anchor) => [anchor.getAttribute('hreflang'), anchor.getAttribute('href')]);

        expect(localeLinks).toEqual([
            ['en', '/en/'],
            ['pt-BR', '/pt-BR/'],
            ['es', '/es/'],
        ]);
    });
});
