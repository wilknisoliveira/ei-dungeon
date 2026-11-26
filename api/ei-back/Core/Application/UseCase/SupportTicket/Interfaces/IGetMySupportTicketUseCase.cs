using ei_back.Core.Application.UseCase.SupportTicket.Dtos;

namespace ei_back.Core.Application.UseCase.SupportTicket.Interfaces;

public interface IGetMySupportTicketUseCase
{
    Task<SupportTicketDetail> Handler(Guid ownerId, Guid ticketId,
        CancellationToken cancellationToken);
}
