import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { LandingFeaturesComponent } from './landing-features.component';

describe('LandingFeaturesComponent', () => {
    let component: LandingFeaturesComponent;
    let fixture: ComponentFixture<LandingFeaturesComponent>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [LandingFeaturesComponent, NoopAnimationsModule],
        });
        fixture = TestBed.createComponent(LandingFeaturesComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render one card per feature', () => {
        const host: HTMLElement = fixture.nativeElement;
        expect(host.querySelectorAll('.feature').length).toBe(
            component.features.length,
        );
    });

    it('should name all nine playable races', () => {
        const races = [
            'Human',
            'Elf',
            'Dwarf',
            'Half-Elf',
            'Halfling',
            'Tiefling',
            'Dragonborn',
            'Half-Orc',
            'Gnome',
        ];
        const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

        races.forEach((race) => {
            expect(text).withContext(race).toContain(race);
        });
    });

    it('should quote the point-buy limits that first-steps enforces', () => {
        const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

        expect(text).toContain('8');
        expect(text).toContain('18');
    });
});
