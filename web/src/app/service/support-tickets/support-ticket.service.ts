import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateSupportTicket, SupportTicketDetail, SupportTicketPage } from 'src/app/types/support-tickets/support-ticket';
import { environment } from 'src/environments/environment';

@Injectable({ providedIn: 'root' })
export class SupportTicketService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.api}/api/support-tickets`;

    list(page: number, pageSize: number): Observable<SupportTicketPage> {
        const params = new HttpParams().set('page', page).set('pageSize', pageSize);
        return this.http.get<SupportTicketPage>(this.baseUrl, { params });
    }

    detail(id: string): Observable<SupportTicketDetail> {
        return this.http.get<SupportTicketDetail>(this.ticketUrl(id));
    }

    create(request: CreateSupportTicket): Observable<SupportTicketDetail> {
        return this.http.post<SupportTicketDetail>(this.baseUrl, request);
    }

    reply(id: string, message: string): Observable<SupportTicketDetail> {
        return this.http.post<SupportTicketDetail>(`${this.ticketUrl(id)}/replies`, { message });
    }

    rate(id: string, rating: number): Observable<SupportTicketDetail> {
        return this.http.post<SupportTicketDetail>(`${this.ticketUrl(id)}/rating`, { rating });
    }

    private ticketUrl(id: string): string {
        return `${this.baseUrl}/${encodeURIComponent(id)}`;
    }
}
