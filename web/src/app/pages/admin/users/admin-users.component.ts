import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDialog } from '@angular/material/dialog';
import { Subscription } from 'rxjs';
import { AdminService } from 'src/app/service/admin/admin.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { AdminQuery, AdminUser, RoleMembership, isLastAdmin, roleForUser } from 'src/app/types/admin/admin';
import { PagedSearch } from 'src/app/types/general/paged-search';
import { AdminRoleDialogComponent } from '../dialogs/admin-role-dialog.component';
import { AdminDeleteDialogComponent } from '../dialogs/admin-delete-dialog.component';

@Component({
    selector: 'app-admin-users',
    standalone: true,
    imports: [MatTableModule, MatPaginatorModule, MatInputModule, MatSelectModule, MatButtonModule,
        MatIconModule, MatMenuModule, MatProgressBarModule],
    templateUrl: './admin-users.component.html',
    styleUrl: './admin-users.component.scss',
})
export class AdminUsersComponent implements OnInit {
    private readonly service = inject(AdminService);
    private readonly dialog = inject(MatDialog);
    private readonly snackbar = inject(SnackbarService);
    private readonly untilDestroyed = takeUntilDestroyed<PagedSearch<AdminUser>>();
    private readonly dialogDestroyed = takeUntilDestroyed<boolean | undefined>();
    private request?: Subscription;
    readonly roles = input<RoleMembership[]>([]);
    readonly rolesReady = input(false);
    readonly changed = output<void>();
    readonly columns = ['email', 'id', 'actions'];
    readonly searchText = signal('');
    readonly query = signal<AdminQuery>({ name: '', sortDirection: 'desc', pageSize: 10, page: 1 });
    readonly users = signal<AdminUser[]>([]);
    readonly total = signal(0);
    readonly loading = signal(false);
    readonly failed = signal(false);
    readonly dialogOpen = signal(false);

    ngOnInit(): void { this.load(); }

    search(): void {
        this.query.update(query => ({ ...query, name: this.searchText().trim(), page: 1 }));
        this.load();
    }

    clearSearch(): void { this.searchText.set(''); this.search(); }

    sort(direction: 'asc' | 'desc'): void {
        this.query.update(query => ({ ...query, sortDirection: direction, page: 1 }));
        this.load();
    }

    paginate(event: PageEvent): void {
        this.query.update(query => ({ ...query, pageSize: event.pageSize,
            page: query.pageSize === event.pageSize ? event.pageIndex + 1 : 1 }));
        this.load();
    }

    load(): void {
        this.request?.unsubscribe();
        this.loading.set(true);
        this.failed.set(false);
        this.request = this.service.listUsers(this.query()).pipe(this.untilDestroyed).subscribe({
            next: result => {
                const lastPage = Math.max(1, Math.ceil(result.totalResults / this.query().pageSize));
                if (this.query().page > lastPage) {
                    this.query.update(query => ({ ...query, page: lastPage }));
                    this.load();
                    return;
                }
                this.users.set(result.items);
                this.total.set(result.totalResults);
                this.loading.set(false);
            },
            error: () => {
                this.users.set([]);
                this.total.set(0);
                this.loading.set(false);
                this.failed.set(true);
            },
        });
    }

    blockReason(user: AdminUser): string {
        if (!this.rolesReady() || roleForUser(user, this.roles()) === null) {
            return 'Account actions require loaded permissions.';
        }
        if (isLastAdmin(user, this.roles())) {
            return 'This is the last administrator. Their account cannot be deleted or assigned another role.';
        }
        return '';
    }

    changeRole(user: AdminUser): void {
        if (this.blockReason(user) || this.dialogOpen() || this.loading()) return;
        const currentRole = roleForUser(user, this.roles());
        if (!currentRole) return;
        this.dialogOpen.set(true);
        this.dialog.open(AdminRoleDialogComponent, {
            data: { user, currentRole, allowDemotion: !isLastAdmin(user, this.roles()) },
            width: '480px', maxWidth: 'calc(100vw - 32px)',
        }).afterClosed().pipe(this.dialogDestroyed).subscribe(result => this.afterWrite(result));
    }

    deleteUser(user: AdminUser): void {
        if (this.blockReason(user) || this.dialogOpen() || this.loading()) return;
        this.dialogOpen.set(true);
        this.dialog.open(AdminDeleteDialogComponent, {
            data: { user }, width: '480px', maxWidth: 'calc(100vw - 32px)',
        }).afterClosed().pipe(this.dialogDestroyed).subscribe(result => this.afterWrite(result));
    }

    private afterWrite(success: boolean | undefined): void {
        this.dialogOpen.set(false);
        if (!success) return;
        this.snackbar.addSuccess('Account updated successfully.');
        this.changed.emit();
        this.load();
    }
}
