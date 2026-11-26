namespace ei_back.Core.Domain.Entity;

public sealed class SupportTicketMessage : Base
{
    private SupportTicketMessage() { }

    internal SupportTicketMessage(Guid ticketId, Guid authorId, string body, bool isFromTeam, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        TicketId = ticketId;
        AuthorId = authorId;
        Body = body;
        IsFromTeam = isFromTeam;
        SetCreatedDate(now);
    }

    public Guid TicketId { get; private set; }
    public Guid? AuthorId { get; private set; }
    public string Body { get; private set; } = null!;
    public bool IsFromTeam { get; private set; }
}
