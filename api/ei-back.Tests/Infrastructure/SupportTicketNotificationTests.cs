using ei_back.Core.Application.Service.Email;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ei_back.Tests.Infrastructure;

public class SupportTicketNotificationTests
{
    [Theory]
    [InlineData(null, "existing@example.com")]
    [InlineData("", "existing@example.com")]
    [InlineData("new@example.com, NEW@example.com", "new@example.com")]
    public void SharedOptions_PrecedeLegacySparksSetting_AndUseConfiguredFrontend(string? recipients, string expected)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Notifications:AdministrativeRecipientsCsv"] = recipients,
            ["Sparks:AdministrativeRecipientsCsv"] = "existing@example.com",
            ["Cors:AllowedOrigins"] = "https://play.example.com"
        }).Build();
        using var provider = new ServiceCollection().AddSingleton<IConfiguration>(configuration)
            .AddServices().BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<AdministrativeNotificationOptions>>().Value;

        options.AdministrativeRecipients.Should().Equal(expected);
        options.WebOrigin.Should().Be("https://play.example.com");
    }

    private static readonly DateTimeOffset At = new DateTimeOffset(2026, 11, 26, 10, 15, 30, TimeSpan.FromHours(-3))
        .AddTicks(1234567);

    [Theory]
    [InlineData("pt-BR", "Correio da Guilda", "Ver resposta", "Em atendimento", "Relato de bug")]
    [InlineData("es", "Correo del Gremio", "Ver respuesta", "En atención", "Informe de error")]
    [InlineData("en", "Guild Mail", "View reply", "In progress", "Bug report")]
    public async Task PlayerReply_UsesOwnersLocaleAndPrivateLink(string language, string mailbox, string action,
        string status, string type)
    {
        var id = Guid.NewGuid();
        var sender = A.Fake<IEmailSender>();
        var service = CreateService(sender);

        await service.NotifyPlayerAsync(new(id, "Connection issue", SupportTicketType.Bug, "player@example.com", language,
            SupportTicketStatus.InProgress, null, true, false, At), default);

        var email = SentEmail(sender);
        email.Recipient.Should().Be("player@example.com");
        email.Subject.Should().Contain(mailbox);
        email.TextBody.Should().Contain("Connection issue").And.Contain(status).And.Contain(type).And.Contain(action)
            .And.Contain($"https://play.example.com/{language}/support-tickets?ticket={id:D}")
            .And.EndWith("2026-11-26 13:15:30.1234567 UTC");
        email.HtmlBody.Should().Contain($"lang=\"{language}\"").And.Contain("EI-DUNGEON")
            .And.Contain("2026-11-26 13:15:30.1234567 UTC");
    }

    [Theory]
    [InlineData(SupportTicketStatus.WaitingForPlayer, false, false, "Reply to the team", "Waiting for your reply")]
    [InlineData(SupportTicketStatus.Closed, true, true, "View conversation and rate", "Closed")]
    [InlineData(SupportTicketStatus.Closed, false, false, "View conversation", "Closed")]
    public async Task StatusTransition_ExplainsBothStatusValuesAndSelectsAction(SupportTicketStatus status,
        bool hasReply, bool canRate, string action, string statusLabel)
    {
        var sender = A.Fake<IEmailSender>();

        await CreateService(sender).NotifyPlayerAsync(new(Guid.NewGuid(), "Help request", SupportTicketType.Help, "player@example.com", "en",
            status, SupportTicketStatus.Open, hasReply, canRate, At), default);

        var email = SentEmail(sender);
        email.TextBody.Should().Contain("Help request").And.Contain("Previous status: Open")
            .And.Contain($"Current status: {statusLabel}").And.Contain($"{action}: ");
        if (status == SupportTicketStatus.Closed)
            email.Subject.Should().Contain("Your conversation is closed");
        if (hasReply)
            email.TextBody.Should().Contain("The Dungeon team replied");
    }

    [Fact]
    public async Task TeamCreation_UsesOneDeliveryAndEscapesTicketSubject()
    {
        var id = Guid.NewGuid();
        var sender = A.Fake<IEmailSender>();

        await CreateService(sender).NotifyTeamAsync(new(id, "<script>private</script>\r\nHeader", SupportTicketType.Bug,
            "player@example.com", true, At), default);

        var email = SentEmail(sender);
        email.Recipient.Should().Be("first@example.com");
        email.BlindCopyRecipients.Should().Equal("second@example.com");
        email.Subject.Should().Be("EI Dungeon: New support ticket");
        email.HtmlBody.Should().Contain("&lt;script&gt;private&lt;/script&gt;").And.NotContain("<script>");
        email.TextBody.Should().Contain("Player: player@example.com").And.Contain("Type: Bug report")
            .And.Contain($"Open ticket: https://play.example.com/en/admin?section=support-tickets&ticket={id:D}");
    }

    [Fact]
    public async Task RepeatedReplies_UseDistinctEventTimesInHtmlAndPlainText()
    {
        var sender = A.Fake<IEmailSender>();
        var service = CreateService(sender);
        var notice = new TeamSupportTicketNotice(Guid.NewGuid(), "Hello", SupportTicketType.Help,
            "player@example.com", false, At);

        await service.NotifyTeamAsync(notice, default);
        await service.NotifyTeamAsync(notice with { UpdatedAt = At.AddTicks(1) }, default);

        var emails = SentEmails(sender).ToList();
        emails.Should().HaveCount(2);
        emails[0].TextBody.Should().Contain("View reply").And.EndWith("2026-11-26 13:15:30.1234567 UTC");
        emails[0].HtmlBody.Should().Contain("2026-11-26 13:15:30.1234567 UTC");
        emails[1].TextBody.Should().EndWith("2026-11-26 13:15:30.1234568 UTC");
        emails[1].HtmlBody.Should().Contain("2026-11-26 13:15:30.1234568 UTC");
    }

    private static OutgoingEmail SentEmail(IEmailSender sender) => SentEmails(sender).Single();

    private static IEnumerable<OutgoingEmail> SentEmails(IEmailSender sender) =>
        Fake.GetCalls(sender).Select(call => call.GetArgument<OutgoingEmail>(0)).OfType<OutgoingEmail>();

    private static SupportTicketNotificationService CreateService(IEmailSender sender) =>
        new(sender, Options.Create(new AdministrativeNotificationOptions
        {
            AdministrativeRecipients = ["first@example.com", "second@example.com"],
            WebOrigin = "https://play.example.com"
        }), NullLogger<SupportTicketNotificationService>.Instance);
}
