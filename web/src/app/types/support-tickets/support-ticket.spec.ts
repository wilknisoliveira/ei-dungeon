import { SUPPORT_TICKET_STATUSES, SUPPORT_TICKET_TYPES, getSupportTicketId } from './support-ticket';

describe('Support ticket contract', () => {
    it('provides the supported ticket types and statuses', () => {
        expect([...SUPPORT_TICKET_TYPES]).toEqual(['Help', 'Bug', 'Problem', 'Suggestion']);
        expect([...SUPPORT_TICKET_STATUSES]).toEqual(['Open', 'InProgress', 'WaitingForPlayer', 'Closed']);
    });

    it('accepts a ticket GUID and rejects values that could identify another route', () => {
        const id = '3617ec15-fae5-4cfc-a613-ddc845970b80';
        expect(getSupportTicketId(id)).toBe(id);
        expect(getSupportTicketId('//example.com')).toBeNull();
        expect(getSupportTicketId(null)).toBeNull();
    });
});
