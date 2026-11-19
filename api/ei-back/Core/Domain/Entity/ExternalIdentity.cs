namespace ei_back.Core.Domain.Entity;

public class ExternalIdentity : Base
{
    public Guid UserId { get; set; }
    public string Provider { get; set; } = "Google";
    public string Subject { get; set; } = string.Empty;
}
