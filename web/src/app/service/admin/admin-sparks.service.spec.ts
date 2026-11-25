import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';
import { AdminSparksService } from './admin-sparks.service';

describe('AdminSparksService', () => {
    let service: AdminSparksService;
    let http: HttpTestingController;
    const url = `${environment.api}/api/admin/sparks`;

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(AdminSparksService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it('requests the selected cycle and propagates a dashboard error', () => {
        const error = jasmine.createSpy('error');
        service.getDashboard('cycle').subscribe({ error });
        const request = http.expectOne(`${url}?cycleId=cycle`);
        expect(request.request.method).toBe('GET');
        request.flush(null, { status: 404, statusText: 'Unavailable' });
        expect(error).toHaveBeenCalled();
    });

    it('sends settings, a capacity increase and a reset to their distinct endpoints', () => {
        const settings = {
            monthlyCapacity: 820, initialGrant: 15, refillLot: 15, estimatedCostPerSpark: 0.0061,
            closingDay: 10, renewalLeadDays: 3,
        };
        service.saveSettings(settings).subscribe();
        const update = http.expectOne({ method: 'PATCH', url: `${url}/settings` });
        expect(update.request.body).toEqual(settings);
        update.flush(null, { status: 204, statusText: 'No Content' });
        service.increase('cycle', 40).subscribe();
        const increase = http.expectOne({ method: 'POST', url: `${url}/cycles/cycle/increases` });
        expect(increase.request.body).toEqual({ amount: 40 });
        increase.flush(null, { status: 204, statusText: 'No Content' });
        service.reset('cycle').subscribe();
        http.expectOne({ method: 'POST', url: `${url}/cycles/cycle/reset` })
            .flush(null, { status: 204, statusText: 'No Content' });
    });
});
