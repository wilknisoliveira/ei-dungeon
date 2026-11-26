import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
    SupportTicketDetail,
    SupportTicketPage,
    SupportTicketStatus,
    SupportTicketType,
} from 'src/app/types/support-tickets/support-ticket';
import { environment } from 'src/environments/environment';

export type SupportTicketSort = 'user' | 'type' | 'status' | 'rating' | 'createdAt' | 'updatedAt';

export interface AdminSupportTicketQuery {
    userEmail?: string;
    type?: SupportTicketType;
    status?: SupportTicketStatus;
    rating?: number | 'unrated';
    createdFrom?: string;
    createdTo?: string;
    updatedFrom?: string;
    updatedTo?: string;
    sortBy: SupportTicketSort;
    sortDirection: 'asc' | 'desc';
    page: number;
    pageSize: number;
}

export interface AdminSupportTicketUpdate {
    type?: SupportTicketType;
    status?: SupportTicketStatus;
    message?: string;
}

@Injectable({ providedIn: 'root' })
export class AdminSupportTicketService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.api}/api/support-tickets/admin`;

    list(query: AdminSupportTicketQuery): Observable<SupportTicketPage> {
        const params = new HttpParams().appendAll(Object.fromEntries(
            Object.entries(query).filter(([, value]) => value !== undefined && value !== ''),
        ));
        return this.http.get<SupportTicketPage>(this.url, { params });
    }

    get(id: string): Observable<SupportTicketDetail> {
        return this.http.get<SupportTicketDetail>(`${this.url}/${encodeURIComponent(id)}`);
    }

    update(id: string, update: AdminSupportTicketUpdate): Observable<SupportTicketDetail> {
        return this.http.patch<SupportTicketDetail>(`${this.url}/${encodeURIComponent(id)}`, update);
    }
}
