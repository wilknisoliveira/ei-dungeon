using ei_back.Core.Application.Service.Email;
using System.Text.RegularExpressions;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.Service.Verification;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Context;
using ei_back.Infrastructure.Context.Repository;
using ei_back.Infrastructure.Verification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ei_back.Tests.Infrastructure;

internal static class VerificationTestSupport
{
    public static EIContext Open() => new(new DbContextOptionsBuilder<EIContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("VERIFICATION_TEST_CONNECTION")).Options);

    public static VerificationFlowService Flow(EIContext db, IEmailSender sender) => new(
        new UserRepository(db), new VerificationCodeRepository(db), new UnitOfWork(db),
        new VerificationCodeService(), new VerificationCodeProtector(Options.Create(
            new VerificationCodeOptions { HashKey = new string('t', 32) })), sender, ei_back.Tests.Infrastructure.LocalizationTestSupport.CreateComposer(),
        NullLogger<VerificationFlowService>.Instance);

    public static IEmailSender Capture(Action<string, string> capture)
    {
        var sender = A.Fake<IEmailSender>();
        A.CallTo(() => sender.SendAsync(A<OutgoingEmail>._, A<CancellationToken>._))
            .Invokes((OutgoingEmail email, CancellationToken token) =>
            {
                using var check = Open();
                check.VerificationCodes.Any(x => x.Email == email.Recipient.ToUpperInvariant()).Should().BeTrue();
                capture(email.Recipient, Regex.Match(email.TextBody, @"\b[0-9]{6}\b").Value);
            });
        return sender;
    }
}