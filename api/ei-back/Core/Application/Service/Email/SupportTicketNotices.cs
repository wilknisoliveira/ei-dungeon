using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.Service.Email;

public sealed record TeamSupportTicketNotice(Guid TicketId, string Subject, SupportTicketType Type, string PlayerEmail,
    bool IsNew, DateTimeOffset UpdatedAt);

public sealed record PlayerSupportTicketNotice(Guid TicketId, string Subject, SupportTicketType Type, string PlayerEmail, string Locale,
    SupportTicketStatus Status, SupportTicketStatus? PreviousStatus, bool HasReply, bool CanRate, DateTimeOffset UpdatedAt);
