using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Email;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.UseCase.SupportTicket;
using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Enums;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.SupportTicket;

public sealed class SupportTicketTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_SavesBeforeNotifyingAndReturnsOnlyPlayerVisibleDetails()
    {
        var ownerId = Guid.NewGuid();
        var repository = A.Fake<ISupportTicketRepository>();
        var users = A.Fake<IUserRepository>();
        var unitOfWork = A.Fake<IUnitOfWork>();
        var notifications = A.Fake<ISupportTicketNotificationService>();
        var clock = A.Fake<TimeProvider>();
        A.CallTo(() => clock.GetUtcNow()).Returns(CreatedAt);
        A.CallTo(() => users.GetUserAndRolesAsync(ownerId, A<CancellationToken>._))
            .Returns(new UserEntity("player@example.com", null, UserRole.CommonUser));

        var result = await new CreateSupportTicketUseCase(repository, users, unitOfWork, notifications, clock)
            .Handler(ownerId, new CreateSupportTicketRequest(" Subject ", SupportTicketType.Help, " First message "), default);

        result.Subject.Should().Be("Subject");
        result.OwnerEmail.Should().BeNull();
        result.Status.Should().Be(SupportTicketStatus.Open);
        result.Messages.Should().ContainSingle().Which.Should()
            .BeEquivalentTo(new { Body = "First message", IsFromTeam = false, CreatedAt });
        A.CallTo(() => unitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => notifications.NotifyTeamAsync(
                new TeamSupportTicketNotice(result.Id, "Subject", SupportTicketType.Help, "player@example.com", true, CreatedAt),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly());
    }

    [Fact]
    public async Task Create_WhenCommitFails_DoesNotNotifyTeam()
    {
        var ownerId = Guid.NewGuid();
        var users = A.Fake<IUserRepository>();
        var unitOfWork = A.Fake<IUnitOfWork>();
        var notifications = A.Fake<ISupportTicketNotificationService>();
        A.CallTo(() => users.GetUserAndRolesAsync(ownerId, A<CancellationToken>._))
            .Returns(new UserEntity("player@example.com", null, UserRole.CommonUser));
        A.CallTo(() => unitOfWork.CommitAsync(A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException());

        var useCase = new CreateSupportTicketUseCase(A.Fake<ISupportTicketRepository>(), users, unitOfWork, notifications,
            TimeProvider.System);
        Func<Task> create = () => useCase.Handler(ownerId, new("Support", SupportTicketType.Help, "Please help"), default);

        await create.Should().ThrowAsync<InvalidOperationException>();
        A.CallTo(() => notifications.NotifyTeamAsync(A<TeamSupportTicketNotice>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public void Rate_OnlyOnceAfterClosureAndKeepsLastActivity()
    {
        var ticket = Ticket();
        var closingTime = CreatedAt.AddHours(1);
        Action prematureRating = () => ticket.Rate(5);
        prematureRating.Should().Throw<SupportTicketException>().Which.Code.Should().Be("support_ticket_rating_unavailable");

        ticket.UpdateAsTeam(Guid.NewGuid(), null, SupportTicketStatus.Closed, "Resolved", closingTime).Should().BeTrue();
        Action reply = () => ticket.Reply("Still broken", closingTime.AddMinutes(1));
        reply.Should().Throw<SupportTicketException>().Which.Code.Should().Be("support_ticket_closed");

        Action invalidRating = () => ticket.Rate(0);
        invalidRating.Should().Throw<SupportTicketException>().Which.Code.Should().Be("support_ticket_invalid");
        ticket.Rate(4);
        ticket.Rating.Should().Be(4);
        ticket.UpdatedAt.Should().Be(closingTime);

        Action repeatRating = () => ticket.Rate(5);
        repeatRating.Should().Throw<SupportTicketException>().Which.Code.Should().Be("support_ticket_rating_unavailable");
        ticket.UpdateAsTeam(Guid.NewGuid(), null, SupportTicketStatus.InProgress, null, closingTime.AddMinutes(1));
        ticket.Rating.Should().Be(4);
    }

    [Fact]
    public void UpdateAsTeam_OnlyNewStatusOrReplyRequiresNotification()
    {
        var ticket = Ticket();
        var administrator = Guid.NewGuid();

        ticket.UpdateAsTeam(administrator, SupportTicketType.Bug, SupportTicketStatus.Open, null, CreatedAt.AddMinutes(1))
            .Should().BeFalse();
        ticket.Type.Should().Be(SupportTicketType.Bug);

        ticket.UpdateAsTeam(administrator, null, SupportTicketStatus.WaitingForPlayer, "Can you describe what happened?",
            CreatedAt.AddMinutes(2)).Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.WaitingForPlayer);
        ticket.Messages.Should().HaveCount(2).And.Contain(message => message.IsFromTeam && message.AuthorId == administrator);
    }

    private static ei_back.Core.Domain.Entity.SupportTicket Ticket() =>
        new(Guid.NewGuid(), "Support", SupportTicketType.Help, "Please help", "en", CreatedAt);
}
