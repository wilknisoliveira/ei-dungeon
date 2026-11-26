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

    it('should link to the project repository in a new tab', () => {
        const host: HTMLElement = fixture.nativeElement;
        const links = host.querySelectorAll<HTMLAnchorElement>('.dev-link');

        expect(links.length).toBe(1);
        expect(links[0]?.getAttribute('href')).toBe(
            'https://github.com/wilknisoliveira/ei-dungeon',
        );
        expect(links[0]?.target).toBe('_blank');
        expect(links[0]?.rel).toBe('noopener noreferrer');
    });

    it('invites visitors to email the developer immediately after the development description', () => {
        const host: HTMLElement = fixture.nativeElement;
        const contact = host.querySelector('.landing-lede + .developer-contact');

        expect(contact?.textContent).toBe('Have an idea for Dungeon or want to talk about the project? Contact the developer');
        const link = contact?.querySelector<HTMLAnchorElement>('a');
        expect(link?.getAttribute('href')).toBe('mailto:wilknis.pro@outlook.com');
        expect(link?.textContent).toBe('Contact the developer');
    });
});
