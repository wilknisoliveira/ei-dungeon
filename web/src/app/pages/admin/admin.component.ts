import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, RouterLink } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { AdminService } from 'src/app/service/admin/admin.service';
import { RoleMembership } from 'src/app/types/admin/admin';
import { getSupportTicketId } from 'src/app/types/support-tickets/support-ticket';
import { IsHandsetService } from 'src/app/shared/responsive/is-handset.service';
import { ThemeToggleComponent } from 'src/app/shared/theme-toggle/theme-toggle.component';
import { AdminUsersComponent } from './users/admin-users.component';
import { AdminRolesComponent } from './roles/admin-roles.component';
import { AdminPaginatorIntl } from './admin-paginator-intl';
import { Subscription } from 'rxjs';
import { AdminSparksComponent } from './sparks/admin-sparks.component';
import { AdminSupportTicketsComponent } from './support-tickets/admin-support-tickets.component';

type AdminSection = 'users' | 'roles' | 'sparks' | 'support-tickets';

@Component({
    selector: 'app-admin',
    standalone: true,
    imports: [RouterLink, MatSidenavModule, MatListModule, MatButtonModule, MatIconModule,
        MatProgressBarModule, ThemeToggleComponent,
        AdminUsersComponent, AdminRolesComponent, AdminSparksComponent, AdminSupportTicketsComponent],
    providers: [{ provide: MatPaginatorIntl, useClass: AdminPaginatorIntl }],
    host: { lang: 'en' },
    templateUrl: './admin.component.html',
    styleUrl: './admin.component.scss',
})
export class AdminComponent implements OnInit {
    private readonly service = inject(AdminService);
    private readonly route = inject(ActivatedRoute);
    private readonly untilDestroyed = takeUntilDestroyed<RoleMembership[]>();
    private readonly paramsUntilDestroyed = takeUntilDestroyed<ParamMap>();
    private roleRequest?: Subscription;
    readonly isHandset = inject(IsHandsetService).isHandset;
    readonly drawerOpened = signal(true);
    readonly section = signal<AdminSection>('users');
    readonly initialSupportTicket = signal<string | null>(null);
    readonly roles = signal<RoleMembership[]>([]);
    readonly rolesLoading = signal(false);
    readonly rolesError = signal(false);
    readonly rolesReady = signal(false);

    constructor() {
        effect(() => this.drawerOpened.set(!this.isHandset()));
    }

    ngOnInit(): void {
        this.loadRoles();
        this.route.queryParamMap.pipe(this.paramsUntilDestroyed).subscribe(params => {
            if (params.get('section') === 'support-tickets') {
                this.section.set('support-tickets');
                this.initialSupportTicket.set(getSupportTicketId(params.get('ticket')));
            }
        });
    }

    selectSection(section: AdminSection): void {
        this.initialSupportTicket.set(null);
        this.section.set(section);
        if (this.isHandset()) this.drawerOpened.set(false);
    }

    loadRoles(): void {
        this.roleRequest?.unsubscribe();
        this.rolesReady.set(false);
        this.rolesLoading.set(true);
        this.rolesError.set(false);
        this.roles.set([]);
        this.roleRequest = this.service.getRoles().pipe(this.untilDestroyed).subscribe({
            next: roles => {
                this.roles.set(roles);
                this.rolesReady.set(true);
                this.rolesLoading.set(false);
            },
            error: () => {
                this.rolesLoading.set(false);
                this.rolesError.set(true);
            },
        });
    }
}
