using ei_back.Core.Application.UseCase.SupportTicket.Dtos;

namespace ei_back.Core.Application.UseCase.SupportTicket.Interfaces;

public interface IReplyToSupportTicketUseCase
{
    Task<SupportTicketDetail> Handler(Guid ownerId, Guid ticketId, ReplyToSupportTicketRequest request,
        CancellationToken cancellationToken);
}
