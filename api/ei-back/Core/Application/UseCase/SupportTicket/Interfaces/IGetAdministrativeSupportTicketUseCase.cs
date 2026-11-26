using ei_back.Core.Application.UseCase.SupportTicket.Dtos;

namespace ei_back.Core.Application.UseCase.SupportTicket.Interfaces;

public interface IGetAdministrativeSupportTicketUseCase
{
    Task<SupportTicketDetail> Handler(Guid ticketId,
        CancellationToken cancellationToken);
}
