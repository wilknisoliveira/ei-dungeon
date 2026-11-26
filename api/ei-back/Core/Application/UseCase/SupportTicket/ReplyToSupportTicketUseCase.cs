using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Application.UseCase.SupportTicket.Interfaces;

namespace ei_back.Core.Application.UseCase.SupportTicket;

public sealed class ReplyToSupportTicketUseCase(ISupportTicketRepository repository, IUnitOfWork unitOfWork,
    ISupportTicketNotificationService notifications, TimeProvider clock) : IReplyToSupportTicketUseCase
{
    public async Task<SupportTicketDetail> Handler(Guid ownerId, Guid ticketId, ReplyToSupportTicketRequest request,
        CancellationToken cancellationToken)
    {
        var ticket = await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var ticket = SupportTicketDetails.RequireFound(await repository.GetForUpdateAsync(ticketId, ownerId, ct));
            ticket.Reply(request.Message, clock.GetUtcNow());
            return ticket;
        }, cancellationToken);

        await notifications.NotifyTeamAsync(new(ticket.Id, ticket.Subject, ticket.Type, ticket.Owner.Email, false,
            ticket.UpdatedAt), cancellationToken);
        return SupportTicketDetails.Map(ticket);
    }
}
