namespace ei_back.Core.Application.Service.Email;

public sealed class AdministrativeNotificationOptions
{
    public const string SectionName = "Notifications";

    public string? AdministrativeRecipientsCsv { get; set; }
    public string? WebOrigin { get; set; }

    public string[] AdministrativeRecipients { get; set; } = [];
}
