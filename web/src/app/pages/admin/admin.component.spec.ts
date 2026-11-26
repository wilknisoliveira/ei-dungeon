import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { Subject, of } from 'rxjs';
import { AdminComponent } from './admin.component';
import { AdminService } from 'src/app/service/admin/admin.service';
import { AdminSupportTicketService } from 'src/app/service/admin/admin-support-ticket.service';
import { IsHandsetService } from 'src/app/shared/responsive/is-handset.service';
import { RoleMembership } from 'src/app/types/admin/admin';
import { signal } from '@angular/core';

describe('AdminComponent', () => {
    const handset = signal(false);
    let service: jasmine.SpyObj<AdminService>;
    let supportTickets: jasmine.SpyObj<AdminSupportTicketService>;
    let requests: Subject<RoleMembership[]>[];

    beforeEach(() => {
        handset.set(false);
        requests = [];
        service = jasmine.createSpyObj('AdminService', ['getRoles', 'listUsers']);
        supportTickets = jasmine.createSpyObj('AdminSupportTicketService', ['list', 'get']);
        service.getRoles.and.callFake(() => {
            const request = new Subject<RoleMembership[]>(); requests.push(request); return request;
        });
        service.listUsers.and.returnValue(of({ currentPage: 1, pageSize: 10, totalResults: 0, sortDirection: 'desc', items: [] }));
        supportTickets.list.and.returnValue(of({ currentPage: 1, pageSize: 10, totalResults: 0, sortDirection: 'desc', items: [] }));
        TestBed.configureTestingModule({ imports: [AdminComponent], providers: [
            provideRouter([{ path: 'admin', component: AdminComponent }]), provideNoopAnimations(),
            { provide: AdminService, useValue: service }, { provide: IsHandsetService, useValue: { isHandset: handset } },
            { provide: AdminSupportTicketService, useValue: supportTickets }] });
    });

    it('keeps mutations unavailable until memberships load and scopes English pagination', () => {
        const fixture = TestBed.createComponent(AdminComponent); fixture.detectChanges();
        const component = fixture.componentInstance;
        expect(component.rolesLoading()).toBeTrue(); expect(component.rolesReady()).toBeFalse();
        expect(fixture.debugElement.injector.get(MatPaginatorIntl).itemsPerPageLabel).toBe('Items per page:');
        expect(fixture.nativeElement.getAttribute('lang')).toBe('en');
        expect(fixture.nativeElement.querySelector('app-language-selector')).toBeNull();
        expect(fixture.nativeElement.querySelector('a[href="#sparks"]')?.textContent).toContain('Sparks');
        requests[0].next([{ name: 'Admin', users: ['admin@example.com'] }]); fixture.detectChanges();
        expect(component.rolesReady()).toBeTrue(); expect(component.rolesLoading()).toBeFalse();
    });

    it('clears a stale membership snapshot on refresh and exposes a retry after failure', () => {
        const fixture = TestBed.createComponent(AdminComponent); fixture.detectChanges();
        const component = fixture.componentInstance;
        requests[0].next([{ name: 'Admin', users: ['admin@example.com'] }]);
        component.loadRoles();
        expect(component.rolesReady()).toBeFalse(); expect(component.roles()).toEqual([]);
        requests[1].error(new Error('offline')); fixture.detectChanges();
        expect(component.rolesError()).toBeTrue();
        expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
        component.loadRoles(); requests[2].next([]);
        expect(component.rolesError()).toBeFalse(); expect(component.rolesReady()).toBeTrue();
    });

    it('switches sections and closes mobile navigation, while desktop stays open', () => {
        const fixture = TestBed.createComponent(AdminComponent); fixture.detectChanges();
        const component = fixture.componentInstance;
        expect(component.drawerOpened()).toBeTrue();
        handset.set(true); fixture.detectChanges();
        component.drawerOpened.set(true); component.selectSection('roles');
        expect(component.section()).toBe('roles'); expect(component.drawerOpened()).toBeFalse();
        handset.set(false); fixture.detectChanges();
        expect(component.drawerOpened()).toBeTrue();
    });

    it('cancels old membership requests and subscriptions on destroy', () => {
        const fixture = TestBed.createComponent(AdminComponent); fixture.detectChanges();
        const component = fixture.componentInstance;
        component.loadRoles();
        requests[0].next([{ name: 'Admin', users: ['stale@example.com'] }]);
        expect(component.rolesReady()).toBeFalse();
        fixture.destroy();
        requests[1].next([{ name: 'Admin', users: ['late@example.com'] }]);
        expect(component.roles()).toEqual([]);
    });

    it('opens the ticket queue independently of the account permissions request', () => {
        const fixture = TestBed.createComponent(AdminComponent);
        fixture.detectChanges();
        const link = fixture.nativeElement.querySelector('a[href="#support-tickets"]') as HTMLAnchorElement;
        expect(link.textContent).toContain('Support tickets');

        link.click();
        fixture.detectChanges();
        expect(supportTickets.list).toHaveBeenCalledTimes(1);
        expect(fixture.nativeElement.querySelector('h1').textContent).toBe('Support tickets');
        expect(fixture.nativeElement.querySelector('[aria-label="Loading permissions"]')).toBeNull();
    });

    it('opens the ticket from a notification link in the admin support section', async () => {
        const id = '353d03cc-3553-44d1-9b66-0687b24ae4a1';
        supportTickets.get.and.returnValue(of({
            id, subject: 'Reported issue', type: 'Bug', status: 'Open', ownerEmail: 'player@example.com',
            createdAt: '2026-11-01T00:00:00Z', updatedAt: '2026-11-01T00:00:00Z', rating: null, messages: [],
        }));
        const harness = await RouterTestingHarness.create(`/admin?section=support-tickets&ticket=${id}`);

        expect(supportTickets.get).toHaveBeenCalledOnceWith(id);
        expect(harness.routeNativeElement?.querySelector('h2')?.textContent).toBe('Reported issue');
    });
});
