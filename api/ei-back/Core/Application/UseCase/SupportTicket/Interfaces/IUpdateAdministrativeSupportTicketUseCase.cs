using ei_back.Core.Application.UseCase.SupportTicket.Dtos;

namespace ei_back.Core.Application.UseCase.SupportTicket.Interfaces;

public interface IUpdateAdministrativeSupportTicketUseCase
{
    Task<SupportTicketDetail> Handler(Guid administratorId, Guid ticketId, UpdateSupportTicketRequest request,
        CancellationToken cancellationToken);
}
