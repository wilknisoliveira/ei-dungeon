namespace ei_back.Core.Application.Service.Email.Interfaces;

/// <summary>Delivers a composed email using the configured provider.</summary>
public interface IEmailSender
{
    Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default);
}
