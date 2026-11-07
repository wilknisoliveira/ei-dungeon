import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { LandingProblemComponent } from './landing-problem.component';

describe('LandingProblemComponent', () => {
    let component: LandingProblemComponent;
    let fixture: ComponentFixture<LandingProblemComponent>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [LandingProblemComponent, NoopAnimationsModule],
        });
        fixture = TestBed.createComponent(LandingProblemComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render one entry per pain', () => {
        const host: HTMLElement = fixture.nativeElement;
        expect(host.querySelectorAll('.pain').length).toBe(
            component.pains.length,
        );
    });

    it('should give every pain a title and a body', () => {
        component.pains.forEach((pain) => {
            expect(pain.title.length).toBeGreaterThan(0);
            expect(pain.body.length).toBeGreaterThan(0);
        });
    });
});
