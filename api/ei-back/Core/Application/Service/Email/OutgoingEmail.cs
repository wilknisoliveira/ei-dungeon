namespace ei_back.Core.Application.Service.Email;

/// <summary>A provider-independent email ready for delivery. Never log its contents.</summary>
public sealed record OutgoingEmail(string Recipient, string Subject, string TextBody, string HtmlBody);
