import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { LandingDevelopmentComponent } from './landing-development.component';

describe('LandingDevelopmentComponent', () => {
    let component: LandingDevelopmentComponent;
    let fixture: ComponentFixture<LandingDevelopmentComponent>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [LandingDevelopmentComponent, NoopAnimationsModule],
        });
        fixture = TestBed.createComponent(LandingDevelopmentComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should link both public repositories', () => {
        const host: HTMLElement = fixture.nativeElement;
        const hrefs = Array.from(
            host.querySelectorAll<HTMLAnchorElement>('.dev-links a'),
        ).map((anchor) => anchor.getAttribute('href'));

        expect(hrefs).toContain(component.webRepo);
        expect(hrefs).toContain(component.apiRepo);
    });

    it('should open repository links safely in a new tab', () => {
        const host: HTMLElement = fixture.nativeElement;

        host.querySelectorAll<HTMLAnchorElement>('.dev-links a').forEach(
            (anchor) => {
                expect(anchor.getAttribute('target')).toBe('_blank');
                expect(anchor.getAttribute('rel')).toBe('noopener noreferrer');
            },
        );
    });
});
