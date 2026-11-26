using ei_back.Core.Application.UseCase.SupportTicket.Dtos;

namespace ei_back.Core.Application.UseCase.SupportTicket.Interfaces;

public interface IRateSupportTicketUseCase
{
    Task<SupportTicketDetail> Handler(Guid ownerId, Guid ticketId, RateSupportTicketRequest request,
        CancellationToken cancellationToken);
}
