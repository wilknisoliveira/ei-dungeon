using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Email;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.Service.Sparks;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Tests.Core.Application.UseCase.User;
using Microsoft.Extensions.Logging.Abstractions;

namespace ei_back.Tests.Core.Application.Service.Email;

public sealed class SparkGrantNotificationTests
{
    [Theory]
    [InlineData("en", "en", "Your Sparks are available", "2026-11-27 at 07:00 -03:00")]
    [InlineData("pt-BR", "pt-BR", "Suas Centelhas estão disponíveis", "27/11/2026 às 07:00 -03:00")]
    [InlineData("es", "es", "Tus Chispas están disponibles", "27/11/2026 a las 07:00 -03:00")]
    public async Task DispatchGrant_UsesSharedTemplateAndPreservesLocaleExpiryAndDeliveryKey(
        string culture, string expectedLanguage, string subject, string expiry)
    {
        var now = new DateTimeOffset(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);
        var player = new User("player@example.com", null, UserRole.CommonUser) { Id = Guid.NewGuid() };
        var request = new SparkQueueRequest(player.Id, SparkGrantKind.Initial, now, culture);
        request.Accept(now, 1);
        request.Fulfill();
        var grant = new SparkGrant(player.Id, Guid.NewGuid(), SparkGrantKind.Initial, 15, now, request.Id);
        var repository = A.Fake<ISparkRepository>();
        var users = A.Fake<IUserRepository>();
        var sender = A.Fake<IEmailSender>();
        var clock = A.Fake<TimeProvider>();
        A.CallTo(() => clock.GetUtcNow()).Returns(now);
        A.CallTo(() => repository.PendingNoticesAsync(now, default)).Returns(new List<SparkQueueRequest> { request });
        A.CallTo(() => repository.PendingReportsAsync(default)).Returns(new List<SparkDailySnapshot>());
        A.CallTo(() => repository.FindGrantForRequestAsync(request.Id, default)).Returns(grant);
        A.CallTo(() => repository.FindRequestAsync(request.Id, default)).Returns(request);
        A.CallTo(() => users.FindByIdAsync(player.Id, default)).Returns(player);

        var notifications = new SparkNotifications(repository, users, new TestUnitOfWork(), sender, clock,
            NullLogger<SparkNotifications>.Instance);
        await notifications.DispatchAsync(default);

        var sent = Fake.GetCalls(sender).Single().GetArgument<OutgoingEmail>(0)
            ?? throw new InvalidOperationException("Expected a Spark grant email.");
        sent.Recipient.Should().Be(player.Email);
        sent.Subject.Should().Be(subject);
        sent.IdempotencyKey.Should().Be($"spark-grant/{request.Id:N}");
        sent.TextBody.Should().Contain("15 ").And.Contain(expiry).And.Contain("America/Sao_Paulo");
        System.Net.WebUtility.HtmlDecode(sent.HtmlBody).Should().Contain(sent.TextBody).And.Contain("EI-DUNGEON");
        sent.HtmlBody.Should().Contain($"lang=\"{expectedLanguage}\"");
        request.NoticeSentAt.Should().Be(now);
    }
}
