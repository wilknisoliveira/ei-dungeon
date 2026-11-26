import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';
import { SupportTicketService } from './support-ticket.service';

describe('SupportTicketService', () => {
    const base = `${environment.api}/api/support-tickets`;
    const id = '3617ec15-fae5-4cfc-a613-ddc845970b80';
    let service: SupportTicketService;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(SupportTicketService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it('requests the specified history page', () => {
        const receive = jasmine.createSpy('receive');
        service.list(2, 10).subscribe(receive);
        const request = http.expectOne(`${base}?page=2&pageSize=10`);
        expect(request.request.method).toBe('GET');
        const page = { currentPage: 2, pageSize: 10, totalResults: 12, sortDirection: 'desc', items: [] };
        request.flush(page);
        expect(receive).toHaveBeenCalledOnceWith(page);
    });

    it('loads the specific conversation and propagates server failures', () => {
        const error = jasmine.createSpy('error');
        service.detail(id).subscribe({ error });
        const request = http.expectOne(`${base}/${id}`);
        expect(request.request.method).toBe('GET');
        request.flush(null, { status: 404, statusText: 'Not Found' });
        expect(error).toHaveBeenCalled();
    });

    it('creates, replies to, and rates via the player endpoints', () => {
        const data = { subject: 'Question', type: 'Help' as const, message: 'Hello' };
        service.create(data).subscribe();
        service.reply(id, 'Thanks').subscribe();
        service.rate(id, 5).subscribe();

        const requests = [
            [base, data],
            [`${base}/${id}/replies`, { message: 'Thanks' }],
            [`${base}/${id}/rating`, { rating: 5 }],
        ] as const;
        for (const [url, body] of requests) {
            const request = http.expectOne(url);
            expect(request.request.method).toBe('POST');
            expect(request.request.body).toEqual(body);
            request.flush({ id });
        }
    });
});
