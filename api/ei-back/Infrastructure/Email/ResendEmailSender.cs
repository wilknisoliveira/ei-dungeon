using ei_back.Core.Application.Service.Email;
using ei_back.Core.Application.Service.Email.Interfaces;
using Microsoft.Extensions.Options;
using Resend;

namespace ei_back.Infrastructure.Email;

public class ResendEmailSender(IResend resend, IOptions<ResendEmailOptions> options) : IEmailSender
{
    public async Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var message = new EmailMessage
        {
            From = options.Value.From,
            Subject = email.Subject,
            TextBody = email.TextBody,
            HtmlBody = email.HtmlBody
        };
        message.To.Add(email.Recipient);
        var response = await resend.EmailSendAsync(message, cancellationToken);
        if (!response.Success)
        {
            // Do not expose provider error text or message content through logs or API responses.
            throw new InvalidOperationException("Resend did not accept the email.");
        }
    }
}
