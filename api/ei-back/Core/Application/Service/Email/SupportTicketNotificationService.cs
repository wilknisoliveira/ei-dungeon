using System.Globalization;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.Service.Email.Templates;
using ei_back.Core.Domain.Enums;
using Microsoft.Extensions.Options;

namespace ei_back.Core.Application.Service.Email;

public sealed class SupportTicketNotificationService(
    IEmailSender sender,
    IOptions<AdministrativeNotificationOptions> options,
    ILogger<SupportTicketNotificationService> logger) : ISupportTicketNotificationService
{
    public async Task NotifyTeamAsync(TeamSupportTicketNotice notice, CancellationToken cancellationToken)
    {
        var recipients = options.Value.AdministrativeRecipients;
        if (recipients.Length == 0)
            return;

        var title = notice.IsNew ? "New support ticket" : "New reply from a player";
        var body = notice.IsNew ? "A player started a private conversation." : "A player replied to a support conversation.";
        var url = $"{options.Value.WebOrigin}/en/admin?section=support-tickets&ticket={notice.TicketId:D}";
        var action = (notice.IsNew ? "Open ticket" : "View reply", url);
        var details = new List<(string Label, string Value)> { ("Subject", notice.Subject) };
        if (notice.IsNew)
            details.Add(("Type", SupportTicketEmailCopy.For("en").FormatType(notice.Type)));
        details.Add(("Player", notice.PlayerEmail));

        var email = BuildEmail(recipients[0], "en", $"EI Dungeon: {title}", title, body, details, action,
            ("Updated", FormatTimestamp(notice.UpdatedAt))) with { BlindCopyRecipients = recipients[1..] };
        await DeliverAsync(email, notice.TicketId, cancellationToken);
    }

    public Task NotifyPlayerAsync(PlayerSupportTicketNotice notice, CancellationToken cancellationToken)
    {
        var language = notice.Locale is "pt-BR" or "es" ? notice.Locale : "en";
        var copy = SupportTicketEmailCopy.For(language);
        var closed = notice.PreviousStatus.HasValue && notice.Status == SupportTicketStatus.Closed;
        var title = closed ? copy.ClosedTitle : notice.HasReply ? copy.ReplyTitle : copy.StatusTitle;
        var body = notice.HasReply ? copy.ReplyBody : copy.StatusBody;
        var actionLabel = (closed, notice.Status, notice.PreviousStatus.HasValue, notice.HasReply) switch
        {
            (true, _, _, _) => notice.CanRate ? copy.ViewAndRate : copy.ViewConversation,
            (_, SupportTicketStatus.WaitingForPlayer, true, _) => copy.ReplyToTeam,
            (_, _, _, true) => copy.ViewReply,
            _ => copy.ViewConversation
        };

        var details = new List<(string Label, string Value)>
        {
            (copy.SubjectLabel, notice.Subject),
            (copy.TypeLabel, copy.FormatType(notice.Type))
        };
        if (notice.PreviousStatus is { } previousStatus)
            details.Add((copy.PreviousStatusLabel, copy.FormatStatus(previousStatus)));
        details.Add((copy.StatusLabel, copy.FormatStatus(notice.Status)));

        var url = $"{options.Value.WebOrigin}/{language}/support-tickets?ticket={notice.TicketId:D}";
        var email = BuildEmail(notice.PlayerEmail, language, $"{copy.Mailbox} — {title}", title, body, details,
            (actionLabel, url), (copy.UpdatedLabel, FormatTimestamp(notice.UpdatedAt)));
        return DeliverAsync(email, notice.TicketId, cancellationToken);
    }

    private async Task DeliverAsync(OutgoingEmail email, Guid ticketId, CancellationToken cancellationToken)
    {
        try
        {
            await sender.SendAsync(email, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The ticket change was committed before notification was attempted.
        }
        catch (Exception exception)
        {
            logger.LogWarning("Support ticket notification failed for {TicketId} ({ErrorType})",
                ticketId, exception.GetType().Name);
        }
    }

    private static OutgoingEmail BuildEmail(string recipient, string language, string subject, string title, string body,
        List<(string Label, string Value)> details, (string Label, string Url) action, (string Label, string Value) updated)
    {
        var summary = string.Join("\n", details.Select(detail => $"{detail.Label}: {detail.Value}"));
        var text = $"{title}\n\n{body}\n\n{summary}\n\n{action.Label}: {action.Url}\n\n{updated.Label}: {updated.Value}";
        var html = NotificationEmailTemplate.Render(language, title, body, details.ToArray(), action, updated);
        return new OutgoingEmail(recipient, subject, text, html);
    }

    private static string FormatTimestamp(DateTimeOffset timestamp) =>
        timestamp.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff 'UTC'", CultureInfo.InvariantCulture);
}
