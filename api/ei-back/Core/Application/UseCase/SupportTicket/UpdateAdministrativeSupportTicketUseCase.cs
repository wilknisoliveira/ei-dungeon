using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Application.UseCase.SupportTicket.Interfaces;

namespace ei_back.Core.Application.UseCase.SupportTicket;

public sealed class UpdateAdministrativeSupportTicketUseCase(ISupportTicketRepository repository,
    IUnitOfWork unitOfWork, ISupportTicketNotificationService notifications, TimeProvider clock)
    : IUpdateAdministrativeSupportTicketUseCase
{
    public async Task<SupportTicketDetail> Handler(Guid administratorId, Guid ticketId, UpdateSupportTicketRequest request,
        CancellationToken cancellationToken)
    {
        var (ticket, previousStatus, shouldNotify) = await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var ticket = SupportTicketDetails.RequireFound(await repository.GetForUpdateAsync(ticketId, null, ct));
            var previousStatus = ticket.Status;
            var shouldNotify = ticket.UpdateAsTeam(administratorId, request.Type, request.Status, request.Message,
                clock.GetUtcNow());
            return (ticket, previousStatus, shouldNotify);
        }, cancellationToken);

        if (shouldNotify)
            await notifications.NotifyPlayerAsync(new(ticket.Id, ticket.Subject, ticket.Type, ticket.Owner.Email, ticket.Locale,
                ticket.Status, previousStatus == ticket.Status ? null : previousStatus, request.Message != null,
                !ticket.Rating.HasValue, ticket.UpdatedAt), cancellationToken);

        return SupportTicketDetails.Map(ticket, includeOwner: true);
    }
}
