using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Domain.Entity;

public sealed class SupportTicket : Base
{
    public const int SubjectMaxLength = 200;
    public const int MessageMaxLength = 8000;

    private readonly List<SupportTicketMessage> _messages = [];

    private SupportTicket() { }

    public SupportTicket(Guid ownerId, string subject, SupportTicketType type, string message, string locale,
        DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        OwnerId = ownerId;
        Subject = RequiredText(subject, SubjectMaxLength, "Subject");
        Type = ValidateType(type);
        Locale = locale;
        Status = SupportTicketStatus.Open;
        SetCreatedDate(now);
        AddMessage(ownerId, message, false, now);
    }

    public Guid OwnerId { get; private set; }
    public User Owner { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public SupportTicketType Type { get; private set; }
    public SupportTicketStatus Status { get; private set; }
    public string Locale { get; private set; } = null!;
    public int? Rating { get; private set; }
    public IReadOnlyCollection<SupportTicketMessage> Messages => _messages;

    public void Reply(string message, DateTimeOffset now)
    {
        if (Status == SupportTicketStatus.Closed)
            throw new SupportTicketException("support_ticket_closed", "This conversation is closed.");

        AddMessage(OwnerId, message, false, now);
        SetUpdatedDate(now);
    }

    public void Rate(int rating)
    {
        if (rating is < 1 or > 5)
            throw new SupportTicketException("support_ticket_invalid", "The rating must be between 1 and 5.");
        if (Status != SupportTicketStatus.Closed || Rating.HasValue)
            throw new SupportTicketException("support_ticket_rating_unavailable", "A rating is only accepted once for a closed conversation.");

        Rating = rating;
    }

    public bool UpdateAsTeam(Guid authorId, SupportTicketType? type, SupportTicketStatus? status,
        string? message, DateTimeOffset now)
    {
        if (status is { } requestedStatus && !Enum.IsDefined(requestedStatus))
            throw new SupportTicketException("support_ticket_invalid", "Unknown status.");

        var updatedType = type is { } requestedType ? ValidateType(requestedType) : Type;
        var statusChanged = status.HasValue && status.Value != Status;
        var messageAdded = message != null;

        if (messageAdded)
            AddMessage(authorId, message!, true, now);

        if (updatedType != Type || statusChanged || messageAdded)
        {
            Type = updatedType;
            Status = status ?? Status;
            SetUpdatedDate(now);
        }

        return statusChanged || messageAdded;
    }

    private void AddMessage(Guid authorId, string message, bool isFromTeam, DateTimeOffset now) =>
        _messages.Add(new SupportTicketMessage(Id, authorId, RequiredText(message, MessageMaxLength, "Message"),
            isFromTeam, now));

    private static SupportTicketType ValidateType(SupportTicketType type) =>
        Enum.IsDefined(type) ? type : throw new SupportTicketException("support_ticket_invalid", "Unknown conversation type.");

    private static string RequiredText(string? value, int maxLength, string field)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text) || text.Length > maxLength)
            throw new SupportTicketException("support_ticket_invalid", $"{field} must have between 1 and {maxLength} characters.");

        return text;
    }
}
