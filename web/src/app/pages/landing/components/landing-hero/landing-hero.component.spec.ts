import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { LandingHeroComponent } from './landing-hero.component';

describe('LandingHeroComponent', () => {
    let component: LandingHeroComponent;
    let fixture: ComponentFixture<LandingHeroComponent>;

    const host = (): HTMLElement => fixture.nativeElement;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [
                LandingHeroComponent,
                RouterTestingModule,
                NoopAnimationsModule,
            ],
        });
        fixture = TestBed.createComponent(LandingHeroComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    describe('logged out', () => {
        beforeEach(() => {
            fixture.componentRef.setInput('isLoggedIn', false);
            fixture.detectChanges();
        });

        it('should send the primary call to action to login with Sparks intent', () => {
            expect(
                host().querySelector('.cta-primary')?.getAttribute('href'),
            ).toBe('/login?sparks=1');
        });

        it('should offer a secondary route to sign up with Sparks intent', () => {
            expect(
                host().querySelector('.cta-secondary')?.getAttribute('href'),
            ).toBe('/signup?sparks=1');
        });

        it('should show the amount of the initial grant', () => {
            expect(host().querySelector('.hero-note')?.textContent).toContain('15 Sparks');
        });
    });

    describe('logged in', () => {
        beforeEach(() => {
            fixture.componentRef.setInput('isLoggedIn', true);
            fixture.detectChanges();
        });

        it('should send the primary call to action to the Sparks panel on Home', () => {
            expect(
                host().querySelector('.cta-primary')?.getAttribute('href'),
            ).toBe('/home?sparks=1');
        });

        it('should not offer signup or login', () => {
            const hrefs = Array.from(
                host().querySelectorAll<HTMLAnchorElement>('.hero-actions a'),
            ).map((anchor) => anchor.getAttribute('href'));

            expect(hrefs).toEqual(['/home?sparks=1']);
        });

        it('should show the amount of the initial grant', () => {
            expect(host().querySelector('.hero-note')?.textContent).toContain('15 Sparks');
        });
    });

    it('should show the full wordmark with localized alt text', () => {
        const logo = host().querySelector<HTMLImageElement>('.wordmark');

        expect(logo?.getAttribute('src')).toBe('assets/logo_completed.webp');
        expect(logo?.alt.length).toBeGreaterThan(0);
    });

    it('should render exactly one h1', () => {
        expect(host().querySelectorAll('h1').length).toBe(1);
        expect(host().querySelector('h1')?.textContent?.trim()).toBe(
            'Your AI Game Master never cancels.',
        );
    });
});
