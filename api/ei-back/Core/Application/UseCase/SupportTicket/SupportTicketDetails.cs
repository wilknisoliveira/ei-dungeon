using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Domain.DomainExceptions;
using Ticket = ei_back.Core.Domain.Entity.SupportTicket;

namespace ei_back.Core.Application.UseCase.SupportTicket;

internal static class SupportTicketDetails
{
    public static Ticket RequireFound(Ticket? ticket) =>
        ticket ?? throw new SupportTicketException("support_ticket_not_found", "Conversation not found.");

    public static SupportTicketDetail Map(Ticket ticket, bool includeOwner = false) =>
        new(ticket.Id, ticket.Subject, ticket.Type, ticket.Status, ticket.CreatedAt, ticket.UpdatedAt, ticket.Rating,
            includeOwner ? ticket.Owner.Email : null,
            ticket.Messages.OrderBy(message => message.CreatedAt).ThenBy(message => message.Id)
                .Select(message => new SupportTicketMessageDto(message.Id, message.Body, message.CreatedAt,
                    message.IsFromTeam)).ToArray());
}
