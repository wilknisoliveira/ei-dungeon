import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Subject, of, throwError } from 'rxjs';
import { AdminService } from 'src/app/service/admin/admin.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { AdminDeleteDialogComponent } from './admin-delete-dialog.component';

describe('AdminDeleteDialogComponent', () => {
    let fixture: ComponentFixture<AdminDeleteDialogComponent>;
    let component: AdminDeleteDialogComponent;
    let service: jasmine.SpyObj<AdminService>;
    let snackbar: jasmine.SpyObj<SnackbarService>;
    let ref: { close: jasmine.Spy; disableClose: boolean };

    beforeEach(async () => {
        service = jasmine.createSpyObj<AdminService>('AdminService', ['deleteUser']);
        snackbar = jasmine.createSpyObj<SnackbarService>('SnackbarService', ['addError']);
        ref = { close: jasmine.createSpy('close'), disableClose: false };
        await TestBed.configureTestingModule({
            imports: [AdminDeleteDialogComponent],
            providers: [provideNoopAnimations(), { provide: MAT_DIALOG_DATA, useValue: { user: { id: 'guid', email: 'member@example.com' } } },
                { provide: MatDialogRef, useValue: ref }, { provide: AdminService, useValue: service },
                { provide: SnackbarService, useValue: snackbar }],
        }).compileComponents();
        fixture = TestBed.createComponent(AdminDeleteDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('warns about irreversible associated-data loss and rejects missing/wrong email', () => {
        expect(fixture.nativeElement.textContent).toContain('member@example.com');
        expect(fixture.nativeElement.textContent).toContain('cannot be undone');
        component.deleteAccount();
        component.confirmation.setValue('someone@example.com');
        component.deleteAccount();
        expect(component.canDelete()).toBeFalse();
        expect(service.deleteUser).not.toHaveBeenCalled();
    });

    it('deletes by GUID only after matching email confirmation', () => {
        service.deleteUser.and.returnValue(of(null));
        component.confirmation.setValue(' member@example.com ');
        component.deleteAccount();
        expect(service.deleteUser).toHaveBeenCalledOnceWith('guid');
        expect(ref.close).toHaveBeenCalledOnceWith(true);
    });

    it('prevents duplicate requests and closing while pending', () => {
        const result = new Subject<unknown>();
        service.deleteUser.and.returnValue(result);
        component.confirmation.setValue('member@example.com');
        component.deleteAccount();
        component.deleteAccount();
        component.cancel();
        expect(service.deleteUser).toHaveBeenCalledTimes(1);
        expect(component.pending()).toBeTrue();
        expect(ref.disableClose).toBeTrue();
        expect(ref.close).not.toHaveBeenCalled();
        result.next(null);
        result.complete();
        expect(ref.close).toHaveBeenCalledOnceWith(true);
    });

    it('does not close on failure, preserves confirmation and allows retry', () => {
        service.deleteUser.and.returnValue(throwError(() => new Error('private server data')));
        component.confirmation.setValue('member@example.com');
        component.deleteAccount();
        fixture.detectChanges();
        expect(component.pending()).toBeFalse();
        expect(ref.disableClose).toBeFalse();
        expect(ref.close).not.toHaveBeenCalled();
        expect(component.confirmation.value).toBe('member@example.com');
        expect(component.canDelete()).toBeTrue();
        expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('Could not delete');
        expect(fixture.nativeElement.textContent).not.toContain('private server data');
        expect(snackbar.addError).toHaveBeenCalled();
    });

    it('cancels without deleting', () => {
        component.cancel();
        expect(ref.close).toHaveBeenCalledOnceWith(false);
        expect(service.deleteUser).not.toHaveBeenCalled();
    });
});
