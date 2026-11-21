import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { finalize } from 'rxjs';
import { AdminService } from 'src/app/service/admin/admin.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { ADMIN_ROLES, AdminRole, AdminUser, roleLabel } from 'src/app/types/admin/admin';

export interface AdminRoleDialogData {
    user: AdminUser;
    currentRole: AdminRole;
    allowDemotion: boolean;
}

@Component({
    selector: 'app-admin-role-dialog',
    host: { lang: 'en' },
    standalone: true,
    imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatSelectModule, MatProgressBarModule],
    templateUrl: './admin-role-dialog.component.html',
    styleUrls: ['./admin-role-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminRoleDialogComponent {
    readonly data = inject<AdminRoleDialogData>(MAT_DIALOG_DATA);
    private readonly dialogRef = inject(MatDialogRef<AdminRoleDialogComponent, boolean>);
    private readonly service = inject(AdminService);
    private readonly snackbar = inject(SnackbarService);
    private readonly destroyRef = inject(DestroyRef);
    readonly role = new FormControl<AdminRole>(this.data.currentRole, { nonNullable: true });
    readonly roles = ADMIN_ROLES;
    readonly roleLabel = roleLabel;
    readonly pending = signal(false);
    readonly error = signal('');

    canSave(): boolean {
        return !this.pending() && this.role.value !== this.data.currentRole
            && this.roles.includes(this.role.value)
            && (this.data.allowDemotion || this.role.value === 'Admin');
    }

    save(): void {
        if (!this.canSave()) return;
        this.pending.set(true);
        this.error.set('');
        this.role.disable({ emitEvent: false });
        this.dialogRef.disableClose = true;
        this.service.applyRole(this.data.user.id, this.role.value).pipe(
            takeUntilDestroyed(this.destroyRef),
            finalize(() => {
                this.pending.set(false);
                this.role.enable({ emitEvent: false });
                this.dialogRef.disableClose = false;
            }),
        ).subscribe({
            next: () => this.dialogRef.close(true),
            error: () => {
                const message = 'Could not change the role. Please try again.';
                this.error.set(message);
                this.snackbar.addError(message);
            },
        });
    }

    cancel(): void {
        if (!this.pending()) this.dialogRef.close(false);
    }
}
