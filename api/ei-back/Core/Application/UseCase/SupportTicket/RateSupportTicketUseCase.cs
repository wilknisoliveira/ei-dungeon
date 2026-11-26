using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Application.UseCase.SupportTicket.Interfaces;

namespace ei_back.Core.Application.UseCase.SupportTicket;

public sealed class RateSupportTicketUseCase(ISupportTicketRepository repository, IUnitOfWork unitOfWork)
    : IRateSupportTicketUseCase
{
    public async Task<SupportTicketDetail> Handler(Guid ownerId, Guid ticketId, RateSupportTicketRequest request,
        CancellationToken cancellationToken)
    {
        var ticket = await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var ticket = SupportTicketDetails.RequireFound(await repository.GetForUpdateAsync(ticketId, ownerId, ct));
            ticket.Rate(request.Rating);
            return ticket;
        }, cancellationToken);

        return SupportTicketDetails.Map(ticket);
    }
}
