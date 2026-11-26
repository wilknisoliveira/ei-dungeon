namespace ei_back.Core.Application.Service.Email.Interfaces;

public interface ISupportTicketNotificationService
{
    Task NotifyTeamAsync(TeamSupportTicketNotice notice, CancellationToken cancellationToken);

    Task NotifyPlayerAsync(PlayerSupportTicketNotice notice, CancellationToken cancellationToken);
}
