import { Component, computed, input, signal } from '@angular/core';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { ADMIN_ROLES, AdminRole, RoleMembership, roleLabel } from 'src/app/types/admin/admin';

interface GroupQuery {
    search: string;
    pageIndex: number;
    pageSize: number;
}

@Component({
    selector: 'app-admin-roles',
    standalone: true,
    imports: [MatExpansionModule, MatFormFieldModule, MatInputModule, MatPaginatorModule, MatTableModule],
    templateUrl: './admin-roles.component.html',
    styleUrl: './admin-roles.component.scss',
})
export class AdminRolesComponent {
    readonly roles = input<RoleMembership[]>([]);
    readonly columns = ['email'];
    private readonly queries = signal<Partial<Record<AdminRole, GroupQuery>>>({});
    readonly groups = computed(() => ADMIN_ROLES.map(role => {
        const query = this.queries()[role] ?? { search: '', pageIndex: 0, pageSize: 10 };
        const members = this.roles().find(group => group.name === role)?.users ?? [];
        const search = query.search.trim().toLocaleLowerCase();
        const filtered = members.filter(email => email.toLocaleLowerCase().includes(search));
        const pageIndex = Math.min(query.pageIndex, Math.max(0, Math.ceil(filtered.length / query.pageSize) - 1));
        return {
            role,
            label: roleLabel(role),
            count: members.length,
            total: filtered.length,
            search: query.search,
            pageIndex,
            pageSize: query.pageSize,
            rows: filtered.slice(pageIndex * query.pageSize, (pageIndex + 1) * query.pageSize),
        };
    }));

    filter(role: AdminRole, search: string): void {
        this.queries.update(queries => ({
            ...queries,
            [role]: { ...this.queryFor(role), search, pageIndex: 0 },
        }));
    }

    page(role: AdminRole, event: PageEvent): void {
        const query = this.queryFor(role);
        this.queries.update(queries => ({
            ...queries,
            [role]: {
                ...query,
                pageSize: event.pageSize,
                pageIndex: event.pageSize === query.pageSize ? event.pageIndex : 0,
            },
        }));
    }

    private queryFor(role: AdminRole): GroupQuery {
        return this.queries()[role] ?? { search: '', pageIndex: 0, pageSize: 10 };
    }
}
