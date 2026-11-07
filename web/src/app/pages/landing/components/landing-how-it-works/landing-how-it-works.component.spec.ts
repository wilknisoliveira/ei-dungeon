import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { LandingHowItWorksComponent } from './landing-how-it-works.component';

describe('LandingHowItWorksComponent', () => {
    let component: LandingHowItWorksComponent;
    let fixture: ComponentFixture<LandingHowItWorksComponent>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [LandingHowItWorksComponent, NoopAnimationsModule],
        });
        fixture = TestBed.createComponent(LandingHowItWorksComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should present exactly three steps', () => {
        const host: HTMLElement = fixture.nativeElement;

        expect(component.steps.length).toBe(3);
        expect(host.querySelectorAll('.node').length).toBe(3);
    });

    it('should number the steps from one', () => {
        const host: HTMLElement = fixture.nativeElement;
        const numbers = Array.from(host.querySelectorAll('.node-number')).map(
            (element) => element.textContent?.trim(),
        );

        expect(numbers).toEqual(['1', '2', '3']);
    });

    it('should render the steps as an ordered list, not a card grid', () => {
        const host: HTMLElement = fixture.nativeElement;

        expect(host.querySelector('ol.timeline')).toBeTruthy();
        expect(host.querySelector('.step')).toBeNull();
    });

    it('should announce itself as the answer to the problem section', () => {
        const host: HTMLElement = fixture.nativeElement;

        expect(host.querySelector('.landing-eyebrow')?.textContent).toContain(
            'The way out',
        );
        expect(host.querySelector('.landing-heading')?.textContent).toContain(
            'None of that is required here',
        );
    });
});
