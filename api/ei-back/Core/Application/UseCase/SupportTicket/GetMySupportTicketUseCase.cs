using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Application.UseCase.SupportTicket.Interfaces;

namespace ei_back.Core.Application.UseCase.SupportTicket;

public sealed class GetMySupportTicketUseCase(ISupportTicketRepository repository) : IGetMySupportTicketUseCase
{
    public async Task<SupportTicketDetail> Handler(Guid ownerId, Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await repository.GetAsync(ticketId, ownerId, cancellationToken);
        return SupportTicketDetails.Map(SupportTicketDetails.RequireFound(ticket));
    }
}
