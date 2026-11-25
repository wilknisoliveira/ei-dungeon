import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { LandingCtaComponent } from './landing-cta.component';

describe('LandingCtaComponent', () => {
    let component: LandingCtaComponent;
    let fixture: ComponentFixture<LandingCtaComponent>;

    const host = (): HTMLElement => fixture.nativeElement;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [
                LandingCtaComponent,
                RouterTestingModule,
                NoopAnimationsModule,
            ],
        });
        fixture = TestBed.createComponent(LandingCtaComponent);
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

        it('should send the final call to action to login with Sparks intent', () => {
            expect(
                host().querySelector('.cta-primary')?.getAttribute('href'),
            ).toBe('/login?sparks=1');
        });
    });

    describe('logged in', () => {
        beforeEach(() => {
            fixture.componentRef.setInput('isLoggedIn', true);
            fixture.detectChanges();
        });

        it('should send the final call to action to the Sparks panel on Home', () => {
            expect(
                host().querySelector('.cta-primary')?.getAttribute('href'),
            ).toBe('/home?sparks=1');
        });
    });
});
