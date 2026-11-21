import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { PageEvent } from '@angular/material/paginator';
import { AdminRolesComponent } from './admin-roles.component';

describe('AdminRolesComponent', () => {
    let fixture: ComponentFixture<AdminRolesComponent>;
    let component: AdminRolesComponent;
    const emails = Array.from({ length: 31 }, (_, index) => `player${index}@example.com`);

    beforeEach(() => {
        TestBed.configureTestingModule({ imports: [AdminRolesComponent], providers: [provideNoopAnimations()] });
        fixture = TestBed.createComponent(AdminRolesComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('roles', [
            { name: 'Admin', users: ['owner@example.com'] },
            { name: 'CommonUser', users: emails },
            { name: 'PremiumUser', users: [] },
        ]);
        fixture.detectChanges();
    });

    it('shows all canonical groups with full membership counts', () => {
        expect(component.groups().map(group => group.role)).toEqual(['Admin', 'CommonUser', 'PremiumUser']);
        expect(component.groups().map(group => group.count)).toEqual([1, 31, 0]);
        expect(fixture.nativeElement.querySelectorAll('mat-expansion-panel').length).toBe(3);
        expect(fixture.nativeElement.textContent).toContain('No accounts found in this role.');
    });

    it('pages a group locally without changing another group', () => {
        component.page('CommonUser', { pageIndex: 1, pageSize: 10, length: 31 } as PageEvent);
        expect(component.groups()[1].rows).toEqual(emails.slice(10, 20));
        expect(component.groups()[0].rows).toEqual(['owner@example.com']);
        expect(component.roles()[1].users.length).toBe(31);
    });

    it('filters email case-insensitively, resets the page and keeps the full count', () => {
        component.page('CommonUser', { pageIndex: 2, pageSize: 10, length: 31 } as PageEvent);
        component.filter('CommonUser', ' PLAYER2@EXAMPLE.COM ');
        const group = component.groups()[1];
        expect(group.rows).toEqual(['player2@example.com']);
        expect(group.total).toBe(1);
        expect(group.count).toBe(31);
        expect(group.pageIndex).toBe(0);
    });

    it('resets to the first page when page size changes', () => {
        component.page('CommonUser', { pageIndex: 2, pageSize: 10, length: 31 } as PageEvent);
        component.page('CommonUser', { pageIndex: 1, pageSize: 25, length: 31 } as PageEvent);
        expect(component.groups()[1].pageIndex).toBe(0);
        expect(component.groups()[1].rows).toEqual(emails.slice(0, 25));
    });

    it('clamps the displayed page after membership refresh and retains a valid filter', () => {
        component.page('CommonUser', { pageIndex: 3, pageSize: 10, length: 31 } as PageEvent);
        fixture.componentRef.setInput('roles', [{ name: 'CommonUser', users: emails.slice(0, 12) }]);
        expect(component.groups()[1].pageIndex).toBe(1);
        expect(component.groups()[1].rows).toEqual(emails.slice(10, 12));
    });

    it('shows a genuine empty state for an unmatched search without dropping the role group', () => {
        component.filter('Admin', 'missing');
        fixture.detectChanges();
        expect(component.groups()[0].total).toBe(0);
        expect(component.groups()[0].count).toBe(1);
        expect(component.groups().length).toBe(3);
    });
});
