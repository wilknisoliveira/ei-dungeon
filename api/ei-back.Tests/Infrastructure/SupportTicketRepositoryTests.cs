using ei_back.Core.Application.UseCase.SupportTicket;
using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Application.Utils;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Context;
using ei_back.Infrastructure.Context.Repository;

namespace ei_back.Tests.Infrastructure;

public sealed class SupportTicketRepositoryTests
{
    [VerificationDatabaseFact]
    public async Task ListAdministrative_AppliesCombinedFiltersBeforePagingAndIncludesWholeEndDate()
    {
        await using var db = VerificationTestSupport.Open();
        var prefix = Guid.NewGuid().ToString("N");
        var owner = AddUser(db, prefix + "owner");
        var other = AddUser(db, Guid.NewGuid().ToString("N"));
        await db.SaveChangesAsync();
        var start = new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);
        var matchingOlder = AddTicket(db, owner.Id, "First", SupportTicketType.Help, start.AddHours(1));
        var matchingNewer = AddTicket(db, owner.Id, "Second", SupportTicketType.Help, start.AddHours(23));
        AddTicket(db, owner.Id, "Different type", SupportTicketType.Bug, start.AddHours(23));
        AddTicket(db, owner.Id, "Outside date", SupportTicketType.Help, start.AddDays(1));
        AddTicket(db, other.Id, "Different owner", SupportTicketType.Help, start.AddHours(22));
        var rated = AddTicket(db, owner.Id, "Rated", SupportTicketType.Help, start.AddHours(23));
        rated.UpdateAsTeam(owner.Id, null, SupportTicketStatus.Closed, null, start.AddHours(23));
        rated.Rate(5);
        await db.SaveChangesAsync();

        var result = await new ListAdministrativeSupportTicketsUseCase(new SupportTicketRepository(db)).Handler(
            new AdministrativeSupportTicketQuery
            {
                UserEmail = prefix.ToUpperInvariant(),
                Type = SupportTicketType.Help,
                Status = SupportTicketStatus.Open,
                Rating = "unrated",
                CreatedFrom = "2026-11-20",
                CreatedTo = "2026-11-20",
                UpdatedFrom = "2026-11-20T00:00:00Z",
                UpdatedTo = "2026-11-20T23:00:00Z",
                SortBy = "createdAt",
                SortDirection = "desc",
                Page = 2,
                PageSize = 1
            }, default);

        result.TotalResults.Should().Be(2);
        result.Items.Should().ContainSingle().Which.Id.Should().Be(matchingOlder.Id);
        var mine = await new SupportTicketRepository(db).ListMineAsync(other.Id, new SupportTicketPageQuery(), default);
        mine.Items.Should().ContainSingle().Which.Id.Should().NotBe(matchingNewer.Id);
        (await new SupportTicketRepository(db).GetAsync(matchingNewer.Id, other.Id, default)).Should().BeNull();
    }

    [VerificationDatabaseFact]
    public async Task Rate_ConcurrentAttemptsCommitExactlyOnce()
    {
        await using var setup = VerificationTestSupport.Open();
        var owner = AddUser(setup, Guid.NewGuid().ToString("N"));
        await setup.SaveChangesAsync();
        var closingTime = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        var ticket = AddTicket(setup, owner.Id, "Subject", SupportTicketType.Problem, closingTime.AddHours(-1));
        ticket.UpdateAsTeam(owner.Id, null, SupportTicketStatus.Closed, null, closingTime);
        await setup.SaveChangesAsync();

        var results = await Task.WhenAll(Enumerable.Range(1, 5).Select(async rating =>
        {
            await using var db = VerificationTestSupport.Open();
            var useCase = new RateSupportTicketUseCase(new SupportTicketRepository(db), new UnitOfWork(db));
            try
            {
                await useCase.Handler(owner.Id, ticket.Id, new(rating), default);
                return true;
            }
            catch (SupportTicketException error) when (error.Code == "support_ticket_rating_unavailable")
            {
                return false;
            }
        }));

        results.Count(result => result).Should().Be(1);
        await using var check = VerificationTestSupport.Open();
        var stored = await new SupportTicketRepository(check).GetAsync(ticket.Id, owner.Id, default);
        stored!.Rating.Should().BeInRange(1, 5);
        stored.UpdatedAt.Should().Be(closingTime);
    }

    internal static User AddUser(EIContext db, string alias)
    {
        var user = new User(alias + "@guild-mail.test", null, UserRole.CommonUser)
        {
            Id = Guid.NewGuid(),
            NormalizedEmail = EmailNormalizer.Normalize(alias + "@guild-mail.test")
        };
        user.SetCreatedDate(DateTimeOffset.UtcNow);
        db.Users.Add(user);
        return user;
    }

    private static SupportTicket AddTicket(EIContext db, Guid ownerId, string subject, SupportTicketType type, DateTimeOffset at)
    {
        var ticket = new SupportTicket(ownerId, subject, type, "Player message", "en", at);
        db.SupportTickets.Add(ticket);
        return ticket;
    }
}
