import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AdminSparksService, SparkDashboard } from 'src/app/service/admin/admin-sparks.service';
import { AdminSparksComponent } from './admin-sparks.component';

describe('AdminSparksComponent', () => {
    let service: jasmine.SpyObj<AdminSparksService>;
    let fixture: ComponentFixture<AdminSparksComponent>;
    const blocked: SparkDashboard = {
        cycle: {
            id: 'cycle', startsAt: '2026-11-10T03:00:00Z', endsAt: '2026-12-10T03:00:00Z',
            initialCapacity: 820, capacity: 820, state: 'Blocked', blockReason: 'InsufficientCredits',
            available: 820, controlLine: 820,
        },
        cycles: [],
        snapshots: [],
        changes: [],
        settings: { monthlyCapacity: 820, initialGrant: 15, refillLot: 15, estimatedCostPerSpark: 0.0061, closingDay: 10, renewalLeadDays: 3 },
        queue: { initialWaiting: 3, refillWaiting: 2, declined: 1, declinedDate: '2026-11-09' },
        openRouter: { dollars: 1.1, estimatedSparks: 180, observedAt: '2026-11-10T03:00:00Z', stale: true },
    };

    beforeEach(() => {
        service = jasmine.createSpyObj('AdminSparksService', ['getDashboard', 'increase', 'saveSettings', 'reset']);
        service.getDashboard.and.returnValue(of(blocked));
        service.increase.and.returnValue(of(undefined));
        service.saveSettings.and.returnValue(of(undefined));
        service.reset.and.returnValue(of(undefined));
        TestBed.configureTestingModule({
            imports: [AdminSparksComponent],
            providers: [provideNoopAnimations(), { provide: AdminSparksService, useValue: service }],
        });
        fixture = TestBed.createComponent(AdminSparksComponent);
    });

    async function initialize(): Promise<void> {
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
    }

    it('distinguishes stale provider credits and confirmed deficit', async () => {
        await initialize();
        const text = fixture.nativeElement.textContent as string;
        expect(text).toContain('Latest check failed');
        expect(text).toContain('180 estimated Sparks');
        expect(text).toContain('Check coverage and reset cycle');
    });

    it('keeps future settings and current-cycle increases independent', async () => {
        await initialize();
        const component = fixture.componentInstance;
        component.settings.controls.monthlyCapacity.setValue(900);
        await component.saveSettings();
        expect(service.saveSettings).toHaveBeenCalledWith({ ...blocked.settings, monthlyCapacity: 900 });
        expect(service.increase).not.toHaveBeenCalled();

        component.addedCapacity.setValue(60);
        await component.increase();
        expect(service.increase).toHaveBeenCalledWith('cycle', 60);
        await component.reset();
        expect(service.reset).toHaveBeenCalledWith('cycle');
    });
});
