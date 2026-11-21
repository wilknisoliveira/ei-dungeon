import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { Subject, of, throwError } from 'rxjs';
import { AdminUsersComponent } from './admin-users.component';
import { AdminService } from 'src/app/service/admin/admin.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { AdminUser, RoleMembership } from 'src/app/types/admin/admin';
import { PagedSearch } from 'src/app/types/general/paged-search';

describe('AdminUsersComponent', () => {
    let fixture: ComponentFixture<AdminUsersComponent>;
    let component: AdminUsersComponent;
    let service: jasmine.SpyObj<AdminService>;
    let dialog: jasmine.SpyObj<MatDialog>;
    let snackbar: jasmine.SpyObj<SnackbarService>;
    const user: AdminUser = { id: 'user-1', email: 'player@example.com' };
    const admin: AdminUser = { id: 'admin-1', email: 'admin@example.com' };
    const memberships: RoleMembership[] = [
        { name: 'Admin', users: [admin.email] },
        { name: 'CommonUser', users: [user.email] },
        { name: 'PremiumUser', users: [] },
    ];
    const page = (items: AdminUser[] = [user], totalResults = items.length): PagedSearch<AdminUser> =>
        ({ items, totalResults, currentPage: 1, pageSize: 10, sortDirection: 'desc' });

    beforeEach(() => {
        service = jasmine.createSpyObj('AdminService', ['listUsers']);
        service.listUsers.and.returnValue(of(page()));
        dialog = jasmine.createSpyObj('MatDialog', ['open']);
        snackbar = jasmine.createSpyObj('SnackbarService', ['addSuccess']);
        TestBed.configureTestingModule({
            imports: [AdminUsersComponent],
            providers: [provideNoopAnimations(), { provide: AdminService, useValue: service },
                { provide: MatDialog, useValue: dialog }, { provide: SnackbarService, useValue: snackbar }],
        });
        fixture = TestBed.createComponent(AdminUsersComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('roles', memberships);
        fixture.componentRef.setInput('rolesReady', true);
        fixture.detectChanges();
    });

    it('loads the default API page and renders only its rows with the supplied total', () => {
        expect(service.listUsers).toHaveBeenCalledWith({ name: '', sortDirection: 'desc', pageSize: 10, page: 1 });
        expect(fixture.nativeElement.textContent).toContain(user.email);
        service.listUsers.and.returnValue(of(page([user], 91)));
        component.load();
        expect(component.total()).toBe(91);
        expect(component.users()).toEqual([user]);
    });

    it('submits partial email through the form and resets to page one', () => {
        component.query.update(query => ({ ...query, page: 4 }));
        const input: HTMLInputElement = fixture.nativeElement.querySelector('input');
        input.value = ' player@ ';
        input.dispatchEvent(new Event('input'));
        fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit', { cancelable: true }));
        expect(service.listUsers.calls.mostRecent().args[0]).toEqual({ name: 'player@', sortDirection: 'desc', page: 1, pageSize: 10 });
    });

    it('clears submitted search and resets ordering/page-size queries correctly', () => {
        component.searchText.set('player'); component.search();
        component.clearSearch();
        expect(component.query().name).toBe('');
        component.sort('asc');
        expect(service.listUsers.calls.mostRecent().args[0].sortDirection).toBe('asc');
        component.paginate({ pageIndex: 4, pageSize: 25, length: 100 });
        expect(component.query().page).toBe(1);
        expect(component.query().pageSize).toBe(25);
    });

    it('maps zero-based paginator events to one-based API pages', () => {
        service.listUsers.and.returnValue(of(page([user], 80)));
        component.paginate({ pageIndex: 2, pageSize: 10, length: 80 });
        expect(service.listUsers.calls.mostRecent().args[0].page).toBe(3);
    });

    it('cancels older requests so late results cannot replace the latest search', () => {
        const old = new Subject<PagedSearch<AdminUser>>();
        const latest = new Subject<PagedSearch<AdminUser>>();
        service.listUsers.and.returnValues(old, latest);
        component.load();
        component.searchText.set('new'); component.search();
        latest.next(page([{ id: 'new', email: 'new@example.com' }]));
        old.next(page([user]));
        expect(component.users()[0].id).toBe('new');
    });

    it('distinguishes errors from empty results and retries without a write', () => {
        service.listUsers.and.returnValue(throwError(() => new Error('unavailable')));
        component.load(); fixture.detectChanges();
        expect(component.failed()).toBeTrue();
        expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
        service.listUsers.and.returnValue(of(page([])));
        component.load(); fixture.detectChanges();
        expect(component.failed()).toBeFalse();
        expect(fixture.nativeElement.textContent).toContain('No users found');
        expect(dialog.open).not.toHaveBeenCalled();
    });

    it('moves to the last valid page after its final row is removed', () => {
        component.query.update(query => ({ ...query, page: 3 }));
        service.listUsers.and.returnValues(of(page([], 20)), of(page([user], 20)));
        component.load();
        expect(component.query().page).toBe(2);
        expect(service.listUsers.calls.mostRecent().args[0].page).toBe(2);
        expect(component.users()).toEqual([user]);
    });

    it('blocks deletion and demotion of the last Admin before opening any dialog', () => {
        component.deleteUser(admin); component.changeRole(admin);
        expect(component.blockReason(admin)).toContain('last administrator');
        expect(dialog.open).not.toHaveBeenCalled();
    });

    it('blocks writes when membership is unavailable or the account is unresolved', () => {
        fixture.componentRef.setInput('rolesReady', false);
        component.deleteUser(user); component.changeRole(user);
        fixture.componentRef.setInput('rolesReady', true);
        component.deleteUser({ id: 'missing', email: 'missing@example.com' });
        expect(dialog.open).not.toHaveBeenCalled();
    });

    it('allows an Admin action when another Admin exists and preserves the GUID', () => {
        fixture.componentRef.setInput('roles', [{ name: 'Admin', users: [admin.email, 'other@example.com'] }]);
        dialog.open.and.returnValue({ afterClosed: () => of(false) } as any);
        component.changeRole(admin);
        const config = dialog.open.calls.mostRecent().args[1] as any;
        expect(config.data.user.id).toBe(admin.id);
        expect(config.data.currentRole).toBe('Admin');
        expect(config.data.allowDemotion).toBeTrue();
    });

    it('refreshes directory and emits membership refresh only after success', () => {
        dialog.open.and.returnValue({ afterClosed: () => of(true) } as any);
        const changed = jasmine.createSpy('changed');
        component.changed.subscribe(changed);
        service.listUsers.calls.reset();
        component.deleteUser(user);
        expect(changed).toHaveBeenCalledTimes(1);
        expect(service.listUsers).toHaveBeenCalledTimes(1);
        expect(snackbar.addSuccess).toHaveBeenCalledTimes(1);
    });

    it('does not refresh canceled dialogs and prevents opening duplicate dialogs', () => {
        const closed = new Subject<boolean>();
        dialog.open.and.returnValue({ afterClosed: () => closed } as any);
        const changed = jasmine.createSpy('changed'); component.changed.subscribe(changed);
        component.changeRole(user); component.deleteUser(user);
        expect(dialog.open).toHaveBeenCalledTimes(1);
        closed.next(false);
        expect(changed).not.toHaveBeenCalled();
        expect(component.dialogOpen()).toBeFalse();
    });
});
