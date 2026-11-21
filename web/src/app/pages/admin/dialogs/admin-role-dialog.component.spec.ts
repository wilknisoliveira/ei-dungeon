import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Subject, of, throwError } from 'rxjs';
import { AdminService } from 'src/app/service/admin/admin.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { AdminRole } from 'src/app/types/admin/admin';
import { AdminRoleDialogComponent, AdminRoleDialogData } from './admin-role-dialog.component';

describe('AdminRoleDialogComponent', () => {
    let fixture: ComponentFixture<AdminRoleDialogComponent>;
    let component: AdminRoleDialogComponent;
    let service: jasmine.SpyObj<AdminService>;
    let snackbar: jasmine.SpyObj<SnackbarService>;
    let ref: { close: jasmine.Spy; disableClose: boolean };
    let data: AdminRoleDialogData;

    beforeEach(async () => {
        data = { user: { id: 'guid', email: 'member@example.com' }, currentRole: 'CommonUser', allowDemotion: true };
        service = jasmine.createSpyObj<AdminService>('AdminService', ['applyRole']);
        snackbar = jasmine.createSpyObj<SnackbarService>('SnackbarService', ['addError']);
        ref = { close: jasmine.createSpy('close'), disableClose: false };
        await TestBed.configureTestingModule({
            imports: [AdminRoleDialogComponent],
            providers: [provideNoopAnimations(), { provide: MAT_DIALOG_DATA, useValue: data },
                { provide: MatDialogRef, useValue: ref }, { provide: AdminService, useValue: service },
                { provide: SnackbarService, useValue: snackbar }],
        }).compileComponents();
        fixture = TestBed.createComponent(AdminRoleDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('identifies the account and current role without sending a no-op request', () => {
        expect(fixture.nativeElement.textContent).toContain('member@example.com');
        expect(fixture.nativeElement.textContent).toContain('Common user');
        component.save();
        expect(service.applyRole).not.toHaveBeenCalled();
    });

    it('saves the selected canonical role and closes only on success', () => {
        service.applyRole.and.returnValue(of({ id: 'guid', role: 'PremiumUser' }));
        component.role.setValue('PremiumUser');
        component.save();
        expect(service.applyRole).toHaveBeenCalledOnceWith('guid', 'PremiumUser');
        expect(ref.close).toHaveBeenCalledOnceWith(true);
    });

    it('prevents duplicate submissions and cancellation while the request is pending', () => {
        const result = new Subject<{ id: string; role: AdminRole }>();
        service.applyRole.and.returnValue(result);
        component.role.setValue('Admin');
        component.save();
        component.save();
        component.cancel();
        expect(component.pending()).toBeTrue();
        expect(ref.disableClose).toBeTrue();
        expect(service.applyRole).toHaveBeenCalledTimes(1);
        expect(ref.close).not.toHaveBeenCalled();
        result.next({ id: 'guid', role: 'Admin' });
        result.complete();
        expect(ref.close).toHaveBeenCalledOnceWith(true);
        expect(component.pending()).toBeFalse();
    });

    it('keeps failed input available for retry and shows a localized error', () => {
        service.applyRole.and.returnValue(throwError(() => new Error('private server error')));
        component.role.setValue('Admin');
        component.save();
        fixture.detectChanges();
        expect(component.role.value).toBe('Admin');
        expect(component.pending()).toBeFalse();
        expect(ref.disableClose).toBeFalse();
        expect(ref.close).not.toHaveBeenCalled();
        expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('Could not change the role');
        expect(fixture.nativeElement.textContent).not.toContain('private server error');
        expect(snackbar.addError).toHaveBeenCalled();
        expect(component.canSave()).toBeTrue();
    });

    it('blocks demotion when the last-admin rule disallows it even if called directly', () => {
        data.currentRole = 'Admin';
        data.allowDemotion = false;
        component.role.setValue('CommonUser');
        component.save();
        expect(component.canSave()).toBeFalse();
        expect(service.applyRole).not.toHaveBeenCalled();
    });

    it('cancels without sending a request', () => {
        component.cancel();
        expect(ref.close).toHaveBeenCalledOnceWith(false);
        expect(service.applyRole).not.toHaveBeenCalled();
    });
});
