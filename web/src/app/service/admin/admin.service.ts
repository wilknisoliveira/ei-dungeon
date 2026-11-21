import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminQuery, AdminRole, AdminUser, RoleMembership } from 'src/app/types/admin/admin';
import { PagedSearch } from 'src/app/types/general/paged-search';
import { environment } from 'src/environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminService {
    private readonly http = inject(HttpClient);
    private readonly userUrl = `${environment.api}/api/user`;

    listUsers(query: AdminQuery): Observable<PagedSearch<AdminUser>> {
        const params = new HttpParams()
            .set('name', query.name)
            .set('sortDirection', query.sortDirection)
            .set('pageSize', query.pageSize)
            .set('page', query.page);
        return this.http.get<PagedSearch<AdminUser>>(this.userUrl, { params });
    }

    getRoles(): Observable<RoleMembership[]> {
        return this.http.get<RoleMembership[]>(`${this.userUrl}/role`);
    }

    applyRole(id: string, role: AdminRole): Observable<{ id: string; role: AdminRole }> {
        return this.http.put<{ id: string; role: AdminRole }>(`${this.userUrl}/role/apply`, { id, role });
    }

    deleteUser(id: string): Observable<unknown> {
        return this.http.delete(`${this.userUrl}/${encodeURIComponent(id)}`);
    }
}
