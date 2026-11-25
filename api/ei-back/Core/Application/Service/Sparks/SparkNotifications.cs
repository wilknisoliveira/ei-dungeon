using System.Globalization;
using System.Net;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Email;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Domain.Entity;

namespace ei_back.Core.Application.Service.Sparks;

public sealed class SparkNotifications(
    ISparkRepository repository,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IEmailSender email,
    TimeProvider clock,
    ILogger<SparkNotifications> logger)
{
    public async Task DispatchAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var pendingNotices = await repository.PendingNoticesAsync(now, cancellationToken);
        foreach (var item in pendingNotices)
            await SendNoticeAsync(item, cancellationToken);

        var pendingReports = await repository.PendingReportsAsync(cancellationToken);
        foreach (var snapshot in pendingReports)
            await SendReportAsync(snapshot, cancellationToken);
    }

    private async Task SendNoticeAsync(SparkQueueRequest item, CancellationToken cancellationToken)
    {
        var grant = await repository.FindGrantForRequestAsync(item.Id, cancellationToken);
        if (grant is null || grant.ExpiresAt <= clock.GetUtcNow())
            return;

        var user = await users.FindByIdAsync(item.UserId, cancellationToken);
        if (user is null)
            return;

        var (subject, body) = FormatNotice(item.CultureName, grant);
        var message = CreateEmail(user.Email, subject, body, $"spark-grant/{item.Id:N}");
        if (!await TrySendAsync(message, cancellationToken))
            return;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await repository.AcquireLockAsync(token);
            var request = await repository.FindRequestAsync(item.Id, token);
            request?.MarkNoticeSent(clock.GetUtcNow());
            return true;
        }, cancellationToken);
    }

    private async Task SendReportAsync(SparkDailySnapshot snapshot, CancellationToken cancellationToken)
    {
        if (snapshot.ReportSubject is null || snapshot.ReportBody is null || snapshot.ReportRecipients.Length == 0 ||
            SparkCalendar.LocalDate(clock.GetUtcNow()) != snapshot.Date)
            return;

        var message = CreateEmail(snapshot.ReportRecipients[0], snapshot.ReportSubject, snapshot.ReportBody,
            $"spark-daily/{snapshot.Id:N}") with { BlindCopyRecipients = snapshot.ReportRecipients.Skip(1).ToArray() };
        if (!await TrySendAsync(message, cancellationToken))
            return;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await repository.AcquireLockAsync(token);
            var stored = await repository.FindSnapshotAsync(snapshot.CycleId, snapshot.Date, token);
            stored?.MarkReportSent(clock.GetUtcNow());
            return true;
        }, cancellationToken);
    }

    private async Task<bool> TrySendAsync(OutgoingEmail message, CancellationToken cancellationToken)
    {
        try
        {
            await email.SendAsync(message, cancellationToken);
            return true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Spark notification delivery failed with {FailureType}; pending delivery can be retried.", exception.GetType().Name);
            return false;
        }
    }

    private static OutgoingEmail CreateEmail(string recipient, string subject, string body, string key) =>
        new(recipient, subject, body, $"<p>{WebUtility.HtmlEncode(body).Replace("\n", "<br>")}</p>") { IdempotencyKey = key };

    private static (string Subject, string Body) FormatNotice(string cultureName, SparkGrant grant)
    {
        var expiry = TimeZoneInfo.ConvertTime(grant.ExpiresAt, SparkCalendar.Zone);
        if (cultureName.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
            return ("Suas Centelhas estão disponíveis",
                $"Você recebeu {grant.Amount} Centelhas no EI Dungeon. Elas vencem em {expiry.ToString("dd/MM/yyyy 'às' HH:mm zzz", CultureInfo.InvariantCulture)} (America/Sao_Paulo).");
        if (cultureName.StartsWith("es", StringComparison.OrdinalIgnoreCase))
            return ("Tus Chispas están disponibles",
                $"Recibiste {grant.Amount} Chispas en EI Dungeon. Vencen el {expiry.ToString("dd/MM/yyyy 'a las' HH:mm zzz", CultureInfo.InvariantCulture)} (America/Sao_Paulo).");
        return ("Your Sparks are available",
            $"You received {grant.Amount} Sparks in EI Dungeon. They expire on {expiry.ToString("yyyy-MM-dd 'at' HH:mm zzz", CultureInfo.InvariantCulture)} (America/Sao_Paulo).");
    }
}
