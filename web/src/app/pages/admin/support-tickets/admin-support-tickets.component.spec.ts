import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Subject, of, throwError } from 'rxjs';
import { AdminSupportTicketService } from 'src/app/service/admin/admin-support-ticket.service';
import { SupportTicketDetail, SupportTicketPage } from 'src/app/types/support-tickets/support-ticket';
import { AdminSupportTicketsComponent } from './admin-support-tickets.component';

describe('AdminSupportTicketsComponent', () => {
    const detail: SupportTicketDetail = {
        id: 'ticket-id',
        subject: 'A problem with my turn',
        type: 'Bug',
        status: 'Open',
        ownerEmail: 'player@example.com',
        createdAt: '2026-11-23T13:30:00Z',
        updatedAt: '2026-11-25T10:00:00Z',
        rating: null,
        messages: [{
            id: 'message-id', body: 'My turn was skipped.', createdAt: '2026-11-23T13:30:00Z', isFromTeam: false,
        }],
    };
    const page: SupportTicketPage = { items: [detail], totalResults: 34, currentPage: 1, pageSize: 10, sortDirection: 'desc' };
    let service: jasmine.SpyObj<AdminSupportTicketService>;
    let fixture: ComponentFixture<AdminSupportTicketsComponent>;
    let component: AdminSupportTicketsComponent;

    beforeEach(() => {
        service = jasmine.createSpyObj<AdminSupportTicketService>(['list', 'get', 'update']);
        service.list.and.returnValue(of(page));
        service.get.and.returnValue(of(detail));
        TestBed.configureTestingModule({
            imports: [AdminSupportTicketsComponent],
            providers: [provideNoopAnimations(), { provide: AdminSupportTicketService, useValue: service }],
        });
        fixture = TestBed.createComponent(AdminSupportTicketsComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('starts with the most recently updated tickets and renders the queue in English', () => {
        expect(service.list).toHaveBeenCalledOnceWith({ sortBy: 'updatedAt', sortDirection: 'desc', page: 1, pageSize: 10 });
        const host: HTMLElement = fixture.nativeElement;
        expect(host.lang).toBe('en');
        expect(host.querySelector('table')?.textContent).toContain('player@example.com');
        expect(host.querySelector('table')?.textContent).toContain('Unrated');
    });

    it('sends paging, sort, and applied filters to the API and restarts paging when filters change', () => {
        component.paginate({ pageIndex: 2, previousPageIndex: 1, pageSize: 10, length: 34 });
        expect(service.list.calls.mostRecent().args[0].page).toBe(3);

        component.filters.patchValue({ userEmail: ' player@example.com ', type: 'Bug', status: 'Open', rating: 'unrated',
            createdFrom: '2026-11-01', updatedTo: '2026-11-25' });
        component.applyFilters();
        expect(service.list.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({
            userEmail: 'player@example.com', type: 'Bug', status: 'Open', rating: 'unrated', page: 1,
            createdFrom: '2026-11-01', updatedTo: '2026-11-25',
        }));

        component.sort({ active: 'rating', direction: 'asc' });
        expect(service.list.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({
            sortBy: 'rating', sortDirection: 'asc', rating: 'unrated', page: 1,
        }));

        component.clearFilters();
        expect(service.list.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({
            sortBy: 'rating', sortDirection: 'asc', rating: undefined, page: 1,
        }));
    });

    it('shows a range validation error without requesting the queue', () => {
        component.filters.patchValue({ createdFrom: '2026-11-25', createdTo: '2026-11-20' });
        component.applyFilters();
        fixture.detectChanges();

        expect(service.list).toHaveBeenCalledTimes(1);
        expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('Each end date');
    });

    it('requests detail, saves reply and status in one request, and refreshes the queue', () => {
        const host: HTMLElement = fixture.nativeElement;
        host.querySelector<HTMLButtonElement>('.ticket-link')?.click();
        fixture.detectChanges();
        expect(service.get).toHaveBeenCalledOnceWith(detail.id);
        expect(host.querySelector('.messages')?.textContent).toContain('My turn was skipped.');

        const request = new Subject<SupportTicketDetail>();
        service.update.and.returnValue(request);
        component.editor.patchValue({ status: 'Closed', message: ' Resolved now. ' });
        component.save();
        component.save();
        expect(service.update).toHaveBeenCalledOnceWith(detail.id, { status: 'Closed', message: 'Resolved now.' });
        request.next({ ...detail, status: 'Closed' });
        fixture.detectChanges();

        expect(component.ticket()?.status).toBe('Closed');
        expect(component.editor.controls.message.value).toBe('');
        expect(component.saved()).toBeTrue();
        expect(service.list).toHaveBeenCalledTimes(2);
        component.back();
        expect(component.selectedId()).toBeNull();
    });

    it('keeps an unsent reply when the save fails', () => {
        component.open(detail.id);
        service.update.and.returnValue(throwError(() => new Error('offline')));
        component.editor.patchValue({ message: 'Try again after a refresh.' });
        component.save();
        fixture.detectChanges();

        expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('could not be saved');
        expect(component.editor.controls.message.value).toBe('Try again after a refresh.');
        expect(component.editor.enabled).toBeTrue();
    });

    it('offers retries after failed list and detail requests', () => {
        service.list.and.returnValue(throwError(() => new Error('offline')));
        component.load();
        fixture.detectChanges();
        expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('tickets could not be loaded');

        service.get.and.returnValue(throwError(() => new Error('offline')));
        component.open(detail.id);
        fixture.detectChanges();
        expect(fixture.nativeElement.querySelector('.ticket-detail [role="alert"]').textContent).toContain('ticket could not be loaded');
        service.get.and.returnValue(of(detail));
        fixture.nativeElement.querySelector('.ticket-detail [role="alert"] button').click();
        fixture.detectChanges();
        expect(component.ticket()?.id).toBe(detail.id);
    });
});
