using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Application.UseCase.SupportTicket.Interfaces;

namespace ei_back.Core.Application.UseCase.SupportTicket;

public sealed class GetAdministrativeSupportTicketUseCase(ISupportTicketRepository repository)
    : IGetAdministrativeSupportTicketUseCase
{
    public async Task<SupportTicketDetail> Handler(Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await repository.GetAsync(ticketId, null, cancellationToken);
        return SupportTicketDetails.Map(SupportTicketDetails.RequireFound(ticket), includeOwner: true);
    }
}
