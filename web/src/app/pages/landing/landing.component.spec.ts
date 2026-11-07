import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { DOCUMENT } from '@angular/common';

import { LandingComponent } from './landing.component';
import { AuthService } from 'src/app/service/auth/auth.service';
import {
    DARK_THEME,
    LIGHT_THEME,
    THEME_STORAGE_KEY,
} from 'src/app/service/theme/theme.service';

describe('LandingComponent', () => {
    let fixture: ComponentFixture<LandingComponent>;
    let document: Document;
    let authService: AuthService;

    const create = (loggedIn: boolean): ComponentFixture<LandingComponent> => {
        spyOn(authService, 'isUserLoggedIn').and.returnValue(loggedIn);
        const created = TestBed.createComponent(LandingComponent);
        created.detectChanges();
        TestBed.tick();
        created.detectChanges();
        return created;
    };

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [
                LandingComponent,
                RouterTestingModule,
                HttpClientTestingModule,
                NoopAnimationsModule,
            ],
        });

        document = TestBed.inject(DOCUMENT);
        authService = TestBed.inject(AuthService);

        localStorage.removeItem(THEME_STORAGE_KEY);
        document.body.classList.remove(DARK_THEME, LIGHT_THEME);
    });

    afterEach(() => {
        localStorage.removeItem(THEME_STORAGE_KEY);
        document.body.classList.remove(DARK_THEME, LIGHT_THEME);
    });

    it('should create', () => {
        fixture = create(false);
        expect(fixture.componentInstance).toBeTruthy();
    });

    it('should start with the anonymous state before the first render', () => {
        spyOn(authService, 'isUserLoggedIn').and.returnValue(true);

        fixture = TestBed.createComponent(LandingComponent);

        expect(fixture.componentInstance.isLoggedIn()).toBeFalse();
        expect(authService.isUserLoggedIn).not.toHaveBeenCalled();
    });

    it('should render every section in order', () => {
        fixture = create(false);
        const host: HTMLElement = fixture.nativeElement;
        const selectors = [
            'app-landing-header',
            'app-landing-hero',
            'app-landing-problem',
            'app-landing-how-it-works',
            'app-landing-features',
            'app-landing-development',
            'app-landing-faq',
            'app-landing-cta',
            'app-landing-footer',
        ];

        selectors.forEach((selector) => {
            expect(host.querySelector(selector))
                .withContext(selector)
                .toBeTruthy();
        });
    });

    it('should render for a logged-in visitor instead of redirecting', () => {
        fixture = create(true);

        expect(fixture.componentInstance.isLoggedIn()).toBeTrue();
        expect(
            (fixture.nativeElement as HTMLElement).querySelector('.landing'),
        ).toBeTruthy();
    });

    [false, true].forEach((loggedIn) => {
        it(`should disable account actions during development for ${loggedIn ? 'a logged-in' : 'an anonymous'} visitor`, () => {
            fixture = create(loggedIn);
            const host: HTMLElement = fixture.nativeElement;
            const actions = Array.from(
                host.querySelectorAll<HTMLButtonElement>('button.cta-primary, button.signin'),
            );

            expect(actions.length).toBe(3);
            actions.forEach((button) => {
                expect(button.disabled).toBeTrue();
                expect(button.textContent?.trim()).toBe('Coming soon');
            });
            expect(host.querySelector('a.cta-primary, a.signin')).toBeNull();
        });
    });

    it('should force the dark theme on init', () => {
        fixture = create(false);
        expect(document.body.classList.contains(DARK_THEME)).toBeTrue();
    });

    it('should not persist the forced dark theme', () => {
        localStorage.setItem(THEME_STORAGE_KEY, LIGHT_THEME);
        fixture = create(false);

        expect(document.body.classList.contains(DARK_THEME)).toBeTrue();
        expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe(LIGHT_THEME);
    });

    it('should restore the stored theme on destroy', () => {
        localStorage.setItem(THEME_STORAGE_KEY, LIGHT_THEME);
        fixture = create(false);
        fixture.destroy();

        expect(document.body.classList.contains(LIGHT_THEME)).toBeTrue();
        expect(document.body.classList.contains(DARK_THEME)).toBeFalse();
    });

    it('should apply indexable landing metadata and remove it when leaving', () => {
        fixture = create(false);

        expect(document.head.querySelector('link[rel="canonical"]')?.getAttribute('href')).toContain(
            '/en/',
        );
        expect(document.head.querySelector('meta[name="robots"]')?.getAttribute('content')).toBe(
            'index, follow',
        );

        fixture.destroy();

        expect(document.head.querySelector('link[rel="canonical"]')).toBeNull();
        expect(document.head.querySelector('meta[name="robots"]')?.getAttribute('content')).toBe(
            'noindex, nofollow',
        );
    });

    it('should not render a theme toggle', () => {
        fixture = create(false);
        expect(
            (fixture.nativeElement as HTMLElement).querySelector(
                'app-theme-toggle',
            ),
        ).toBeNull();
    });
});
