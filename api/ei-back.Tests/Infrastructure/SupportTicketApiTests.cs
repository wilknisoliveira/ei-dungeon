using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ei_back.Core.Application.Service.Email;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Application.Utils;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Context;
using ei_back.Infrastructure.Exceptions;
using ei_back.Infrastructure.Extensions;
using ei_back.Infrastructure.Token;
using ei_back.UserInterface.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ei_back.Tests.Infrastructure;

public sealed class SupportTicketApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [VerificationDatabaseFact]
    public async Task SupportTicketFlow_EnforcesOwnershipAndNotifiesForCommittedUpdates()
    {
        Guid ownerId;
        Guid otherPlayerId;
        Guid administratorId;
        string ownerEmail;
        await using (var setup = VerificationTestSupport.Open())
        {
            var owner = SupportTicketRepositoryTests.AddUser(setup, Guid.NewGuid().ToString("N"));
            ownerId = owner.Id;
            ownerEmail = owner.Email;
            otherPlayerId = SupportTicketRepositoryTests.AddUser(setup, Guid.NewGuid().ToString("N")).Id;
            administratorId = SupportTicketRepositoryTests.AddUser(setup, Guid.NewGuid().ToString("N")).Id;
            await setup.SaveChangesAsync();
        }

        var notifications = A.Fake<ISupportTicketNotificationService>();
        var tokenConfiguration = new TokenConfiguration
        {
            Secret = new string('a', 64), Issuer = "test", Audience = "test", Minutes = 15
        };
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        // Resolve the ticket registrations exercised by this host without loading unrelated integrations.
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = false;
        });
        builder.Logging.ClearProviders();
        builder.Services.AddApplicationLocalization();
        builder.Services.AddControllers().AddApplicationPart(typeof(SupportTicketController).Assembly)
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddExceptionHandler<AppExceptionHandler>();
        builder.Services.AddDbContext<EIContext>(options =>
            options.UseNpgsql(Environment.GetEnvironmentVariable("VERIFICATION_TEST_CONNECTION")));
        builder.Services.AddRepositories();
        builder.Services.AddUseCases(builder.Configuration);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(notifications);
        builder.Services.AddRateLimitingPolicies();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = tokenConfiguration.Issuer,
                ValidateAudience = true, ValidAudience = tokenConfiguration.Audience,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenConfiguration.Secret))
            });
        builder.Services.AddAuthorization(options => options.AddPolicy("Bearer", policy => policy.RequireAuthenticatedUser()));

        await using var app = builder.Build();
        app.UseExceptionHandler(_ => { });
        app.UseRequestLocalization();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        const string tickets = "/api/support-tickets";
        (await client.GetAsync(tickets)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        Authenticate(client, tokenConfiguration, ownerId, "CommonUser");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR");
        (await client.PostAsJsonAsync(tickets, new { subject = "Missing type", message = "Details" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var create = await client.PostAsJsonAsync(tickets, new { subject = "Report", type = "Bug", message = "Details" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await create.Content.ReadFromJsonAsync<SupportTicketDetail>(Json))!;
        var path = $"{tickets}/{created.Id}";
        create.Headers.Location!.AbsolutePath.Should().Be(path);
        (await client.GetAsync(path)).Headers.CacheControl!.NoStore.Should().BeTrue();
        created.OwnerEmail.Should().BeNull();
        created.Messages.Should().ContainSingle().Which.Body.Should().Be("Details");
        (await client.PostAsJsonAsync($"{path}/replies", new { message = "Extra details" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        Authenticate(client, tokenConfiguration, otherPlayerId, "CommonUser");
        (await client.GetAsync(path)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync($"{path}/replies", new { message = "Cannot write" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync($"{path}/rating", new { rating = 5 }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"{tickets}/admin")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.PatchAsJsonAsync($"{tickets}/admin/{created.Id}", new { status = "Closed" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        Authenticate(client, tokenConfiguration, administratorId, "Admin");
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en");
        var queue = (await client.GetFromJsonAsync<PagedSearchDto<SupportTicketListItem>>(
            $"{tickets}/admin?userEmail={Uri.EscapeDataString(ownerEmail)}&type=Bug&status=Open&rating=unrated&page=1&pageSize=10",
            Json))!;
        queue.TotalResults.Should().Be(1);
        queue.Items.Should().ContainSingle().Which.OwnerEmail.Should().Be(ownerEmail);
        var update = await client.PatchAsJsonAsync($"{tickets}/admin/{created.Id}",
            new { status = "Closed", message = "Resolved" });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var closed = (await update.Content.ReadFromJsonAsync<SupportTicketDetail>(Json))!;
        closed.OwnerEmail.Should().Be(ownerEmail);
        closed.Messages.Select(message => message.Body).Should().Equal("Details", "Extra details", "Resolved");
        closed.Messages.Last().IsFromTeam.Should().BeTrue();
        (await client.PatchAsJsonAsync($"{tickets}/admin/{created.Id}", new { type = "Problem" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        Authenticate(client, tokenConfiguration, ownerId, "CommonUser");
        (await client.PostAsJsonAsync($"{path}/replies", new { message = "Already closed" }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.PostAsJsonAsync($"{path}/rating", new { rating = 4 })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync($"{path}/rating", new { rating = 5 }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
        var history = (await client.GetFromJsonAsync<SupportTicketDetail>(path, Json))!;
        history.Rating.Should().Be(4);
        history.Messages.Should().HaveCount(3);
        history.OwnerEmail.Should().BeNull();

        A.CallTo(() => notifications.NotifyTeamAsync(A<TeamSupportTicketNotice>.That.Matches(notice =>
            notice.TicketId == created.Id && notice.IsNew && notice.PlayerEmail == ownerEmail),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => notifications.NotifyTeamAsync(A<TeamSupportTicketNotice>.That.Matches(notice =>
            notice.TicketId == created.Id && !notice.IsNew && notice.PlayerEmail == ownerEmail),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => notifications.NotifyPlayerAsync(A<PlayerSupportTicketNotice>.That.Matches(notice =>
            notice.TicketId == created.Id && notice.PlayerEmail == ownerEmail && notice.Status == SupportTicketStatus.Closed &&
            notice.PreviousStatus == SupportTicketStatus.Open && notice.HasReply && notice.CanRate &&
            notice.Locale == "pt-BR" && notice.Type == SupportTicketType.Bug), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    private static void Authenticate(HttpClient client, TokenConfiguration options, Guid userId, string role) =>
        client.DefaultRequestHeaders.Authorization = new("Bearer", new TokenService(options).GenerateAccessToken(
            [new Claim("sub", userId.ToString()), new Claim("role", role)]));
}
