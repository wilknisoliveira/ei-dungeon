import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { HomeComponent } from './home.component';
import { AuthService } from 'src/app/service/auth/auth.service';
import { Router } from '@angular/router';
import { OverlayContainer } from '@angular/cdk/overlay';
import { GameService } from 'src/app/service/game/game.service';

describe('HomeComponent', () => {
    let component: HomeComponent;
    let fixture: ComponentFixture<HomeComponent>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [
                HomeComponent,
                RouterTestingModule,
                HttpClientTestingModule,
                MatDialogModule,
                MatMenuModule,
                NoopAnimationsModule,
            ],
        });
        spyOn(TestBed.inject(GameService), 'getGames').and.resolveTo({
            currentPage: 1, pageSize: 10, totalResults: 0, sortDirection: 'desc', items: [],
        });
        fixture = TestBed.createComponent(HomeComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    for (const [minutes, expected] of [[60, '60 min left'], [61, '1 h 1 min left'], [120, '2 h left']] as const) {
        it(`formats ${minutes} remaining minutes as "${expected}" in the Sparks toolbar`, () => {
            fixture.destroy();
            spyOn(TestBed.inject(AuthService), 'getRole').and.returnValue('CommonUser');
            fixture = TestBed.createComponent(HomeComponent);
            component = fixture.componentInstance;
            spyOn(component, 'minutesRemaining').and.returnValue(minutes);
            spyOn(component, 'isHandset').and.returnValue(false);
            fixture.detectChanges();

            const timer = fixture.nativeElement.querySelector('.spark-timer');
            expect(timer?.textContent.trim()).toBe(`· ${expected}`);
            expect(timer?.closest('button')?.getAttribute('aria-label')).toBe('Open Sparks balance, 0 available');
        });
    }

    it('offers administration in the profile menu only to Admin', async () => {
        const role = spyOn(TestBed.inject(AuthService), 'getRole').and.returnValue('Admin');
        fixture.detectChanges();
        fixture.nativeElement.querySelector('.profile-button button').click();
        fixture.detectChanges();
        await fixture.whenStable();
        const overlay = TestBed.inject(OverlayContainer).getContainerElement();
        expect(overlay.querySelector('[data-testid="admin-navigation"]')).not.toBeNull();
        role.and.returnValue('CommonUser');
        fixture.detectChanges();
        expect(overlay.querySelector('[data-testid="admin-navigation"]')).toBeNull();
        role.and.returnValue('PremiumUser');
        fixture.detectChanges();
        expect(overlay.querySelector('[data-testid="admin-navigation"]')).toBeNull();
    });

    it('navigates to admin by URL only for the administrative role', () => {
        const role = spyOn(TestBed.inject(AuthService), 'getRole').and.returnValue('Admin');
        const navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
        component.navigateToAdmin();
        expect(navigate).toHaveBeenCalledOnceWith(['admin']);
        role.and.returnValue('CommonUser');
        component.navigateToAdmin();
        expect(navigate).toHaveBeenCalledTimes(1);
    });

    it('opens Guild Mail in a new tab from the profile menu', async () => {
        fixture.nativeElement.querySelector('.profile-button button').click();
        fixture.detectChanges();
        await fixture.whenStable();

        const overlay = TestBed.inject(OverlayContainer).getContainerElement();
        const link = overlay.querySelector<HTMLAnchorElement>('[data-testid="support-navigation"]');
        expect(link?.getAttribute('href')).toBe('/support-tickets');
        expect(link?.target).toBe('_blank');
        expect(link?.rel).toContain('noopener');
    });
});
