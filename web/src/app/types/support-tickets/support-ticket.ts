import { PagedSearch } from 'src/app/types/general/paged-search';

export const SUPPORT_TICKET_TYPES = ['Help', 'Bug', 'Problem', 'Suggestion'] as const;
export const SUPPORT_TICKET_STATUSES = ['Open', 'InProgress', 'WaitingForPlayer', 'Closed'] as const;

export type SupportTicketType = typeof SUPPORT_TICKET_TYPES[number];
export type SupportTicketStatus = typeof SUPPORT_TICKET_STATUSES[number];

export interface SupportTicketSummary {
    id: string;
    subject: string;
    type: SupportTicketType;
    status: SupportTicketStatus;
    createdAt: string;
    updatedAt: string;
    rating: number | null;
    ownerEmail?: string | null;
}

export interface SupportTicketMessage {
    id: string;
    body: string;
    createdAt: string;
    isFromTeam: boolean;
}

export interface SupportTicketDetail extends SupportTicketSummary {
    messages: SupportTicketMessage[];
}

export interface CreateSupportTicket {
    subject: string;
    type: SupportTicketType;
    message: string;
}

export type SupportTicketPage = PagedSearch<SupportTicketSummary>;

export function getSupportTicketId(value: string | null): string | null {
    const guid = /^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i;
    return value && guid.test(value) ? value : null;
}
