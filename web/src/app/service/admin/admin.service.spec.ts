import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';
import { AdminService } from './admin.service';

describe('AdminService', () => {
    let service: AdminService;
    let http: HttpTestingController;
    const url = `${environment.api}/api/user`;
    const query = { name: 'mail', sortDirection: 'desc' as const, pageSize: 25, page: 2 };

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(AdminService);
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => http.verify());

    it('requests paged users with the exact supported query and preserves the response', () => {
        const response = { currentPage: 2, pageSize: 25, totalResults: 40, sortDirection: 'desc', items: [{ id: 'id', email: 'mail@example.com' }] };
        service.listUsers(query).subscribe(result => expect(result).toEqual(response));
        const request = http.expectOne(request => request.url === url);
        expect(request.request.method).toBe('GET');
        expect(request.request.params.keys().sort()).toEqual(['name', 'page', 'pageSize', 'sortDirection']);
        expect(request.request.params.get('name')).toBe('mail');
        expect(request.request.params.get('page')).toBe('2');
        expect(request.request.params.get('pageSize')).toBe('25');
        expect(request.request.params.get('sortDirection')).toBe('desc');
        request.flush(response);
    });

    it('reads complete role membership without invented paging parameters', () => {
        const response = [{ name: 'Admin', users: ['admin@example.com'] }];
        service.getRoles().subscribe(result => expect(result).toEqual(response));
        const request = http.expectOne(`${url}/role`);
        expect(request.request.method).toBe('GET');
        expect(request.request.params.keys()).toEqual([]);
        request.flush(response);
    });

    it('assigns a canonical role using the internal GUID', () => {
        service.applyRole('guid', 'PremiumUser').subscribe(result => expect(result).toEqual({ id: 'guid', role: 'PremiumUser' }));
        const request = http.expectOne(`${url}/role/apply`);
        expect(request.request.method).toBe('PUT');
        expect(request.request.body).toEqual({ id: 'guid', role: 'PremiumUser' });
        request.flush({ id: 'guid', role: 'PremiumUser' });
    });

    it('deletes only the selected internal account ID', () => {
        const next = jasmine.createSpy('next');
        service.deleteUser('guid').subscribe(next);
        const request = http.expectOne(`${url}/guid`);
        expect(request.request.method).toBe('DELETE');
        expect(request.request.body).toBeNull();
        request.flush(null);
        expect(next).toHaveBeenCalled();
    });

    for (const operation of ['list', 'roles', 'apply', 'delete'] as const) {
        it(`propagates ${operation} errors without automatic replay`, () => {
            const error = jasmine.createSpy('error');
            const observable = operation === 'list' ? service.listUsers(query)
                : operation === 'roles' ? service.getRoles()
                : operation === 'apply' ? service.applyRole('guid', 'Admin') : service.deleteUser('guid');
            observable.subscribe({ error });
            const request = http.expectOne(() => true);
            request.flush({}, { status: 403, statusText: 'Forbidden' });
            expect(error).toHaveBeenCalled();
            expect(error.calls.mostRecent().args[0].status).toBe(403);
        });
    }
});
