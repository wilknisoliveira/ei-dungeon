using ei_back.Core.Application.UseCase.SupportTicket.Dtos;

namespace ei_back.Core.Application.UseCase.SupportTicket.Interfaces;

public interface ICreateSupportTicketUseCase
{
    Task<SupportTicketDetail> Handler(Guid ownerId, CreateSupportTicketRequest request,
        CancellationToken cancellationToken);
}
