import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';
import { SparksService, SparkStatus } from './sparks.service';

describe('SparksService', () => {
    let service: SparksService;
    let http: HttpTestingController;
    const url = `${environment.api}/api/sparks`;
    const available: SparkStatus = {
        balance: 0, expiresAt: null, canRequest: true, requestKind: 'Initial',
        queueState: null, queueRequestId: null,
    };

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(SparksService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it('falls back to the default initial grant when the public request fails', async () => {
        const unavailable = service.getInitialGrant();
        http.expectOne(`${url}/availability`).flush(null, { status: 503, statusText: 'Unavailable' });
        expect(await unavailable).toBe(15);
    });

    it('uses the configured initial grant quantity', async () => {
        const initialGrant = service.getInitialGrant();
        http.expectOne(`${url}/availability`).flush({ initialGrant: 20 });
        expect(await initialGrant).toBe(20);
    });

    it('reads status and clears it when leaving the account', async () => {
        const result = service.refresh();
        http.expectOne({ method: 'GET', url }).flush(available);
        await result;
        expect(service.status()).toEqual(available);
        service.clear();
        expect(service.status()).toBeNull();
    });

    it('requests a grant and submits an explicit queue decision', async () => {
        const request = service.request();
        http.expectOne({ method: 'POST', url }).flush({ ...available, queueState: 'Offered', queueRequestId: 'offer-id' });
        await request;
        const decision = service.decide(false);
        const patch = http.expectOne({ method: 'PATCH', url: `${url}/queue/offer-id` });
        expect(patch.request.body).toEqual({ accepted: false });
        patch.flush(available);
        await decision;
        expect(service.status()?.queueState).toBeNull();
    });

    it('exposes request failures and keeps the latest status', async () => {
        const request = service.refresh();
        http.expectOne(url).flush(null, { status: 500, statusText: 'Error' });
        await request;
        expect(service.error()).toBeTrue();
        expect(service.busy()).toBeFalse();
    });
});
