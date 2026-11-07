import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { LandingFaqComponent } from './landing-faq.component';

describe('LandingFaqComponent', () => {
    let component: LandingFaqComponent;
    let fixture: ComponentFixture<LandingFaqComponent>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [LandingFaqComponent, NoopAnimationsModule],
        });
        fixture = TestBed.createComponent(LandingFaqComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should answer at least the six required questions', () => {
        expect(component.entries.length).toBeGreaterThanOrEqual(6);
    });

    it('should render a panel per entry', () => {
        const host: HTMLElement = fixture.nativeElement;
        expect(host.querySelectorAll('mat-expansion-panel').length).toBe(
            component.entries.length,
        );
    });

    it('should give every entry a non-empty question and answer', () => {
        component.entries.forEach((entry) => {
            expect(entry.question.length).toBeGreaterThan(0);
            expect(entry.answer.length).toBeGreaterThan(0);
        });
    });

    it('should state that the game is free to start without naming a price', () => {
        const answer = component.entries[2].answer;

        expect(answer.toLowerCase()).toContain('free');
        expect(answer).not.toMatch(/[$€£R]\s?\d/);
    });
});
