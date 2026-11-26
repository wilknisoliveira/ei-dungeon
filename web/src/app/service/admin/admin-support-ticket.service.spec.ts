import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { AdminSupportTicketQuery, AdminSupportTicketService } from './admin-support-ticket.service';

describe('AdminSupportTicketService', () => {
    const url = `${environment.api}/api/support-tickets/admin`;
    const query: AdminSupportTicketQuery = { sortBy: 'updatedAt', sortDirection: 'desc', page: 1, pageSize: 10 };
    const id = 'ticket-id';
    let service: AdminSupportTicketService;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(AdminSupportTicketService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it('sends all supported filters, ordering, and paging for the API to apply', () => {
        const filtered: AdminSupportTicketQuery = {
            ...query,
            page: 2,
            userEmail: 'player+quests@example.com',
            type: 'Bug',
            status: 'Open',
            rating: 'unrated',
            createdFrom: '2026-11-01',
            createdTo: '2026-11-10',
            updatedFrom: '2026-11-20',
            updatedTo: '2026-11-25',
        };
        const response = { items: [], totalResults: 0, currentPage: 2, pageSize: 10, sortDirection: 'desc' };
        service.list(filtered).subscribe(result => expect(result).toEqual(response));
        const request = http.expectOne(req => req.url === url && req.method === 'GET');

        expect(request.request.params.keys().sort()).toEqual(Object.keys(filtered).sort());
        for (const [key, value] of Object.entries(filtered)) {
            expect(request.request.params.get(key)).toBe(String(value));
        }
        request.flush(response);
    });

    it('omits empty and undefined optional filters', () => {
        service.list({ ...query, userEmail: '', rating: undefined }).subscribe();
        const request = http.expectOne(req => req.url === url);
        expect(request.request.params.keys().sort()).toEqual(Object.keys(query).sort());
        request.flush({ items: [], totalResults: 0, currentPage: 1, pageSize: 10 });
    });

    it('loads one ticket from the admin detail endpoint', () => {
        const detail = { id, subject: 'Quest problem', messages: [] };
        service.get(id).subscribe(result => expect(result).toEqual(jasmine.objectContaining(detail)));
        http.expectOne({ url: `${url}/${id}`, method: 'GET' }).flush(detail);
    });

    it('uses one PATCH for the status, type and reply', () => {
        const update = { status: 'Closed', type: 'Help', message: 'The issue is resolved.' } as const;
        service.update(id, update).subscribe();
        const request = http.expectOne({ url: `${url}/${id}`, method: 'PATCH' });
        expect(request.request.body).toEqual(update);
        request.flush({ id, messages: [] });
    });

    for (const method of ['list', 'get', 'update'] as const) {
        it(`propagates ${method} errors`, () => {
            const error = jasmine.createSpy('error');
            const operation: Observable<unknown> = method === 'list' ? service.list(query)
                : method === 'get' ? service.get(id) : service.update(id, { status: 'Closed' });
            operation.subscribe({ error });
            http.expectOne(() => true).flush(null, { status: 403, statusText: 'Forbidden' });
            expect(error.calls.mostRecent().args[0].status).toBe(403);
        });
    }
});
