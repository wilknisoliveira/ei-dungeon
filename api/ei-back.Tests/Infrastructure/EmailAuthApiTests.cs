using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Application.Repository;
using ei_back.Infrastructure.Exceptions;
using ei_back.Infrastructure.Extensions;
using ei_back.UserInterface.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ei_back.Tests.Infrastructure;

public class EmailAuthApiTests
{
    [Fact]
    public async Task ActualMvcContracts_ValidatePayloadsAndReturnEmptySuccessesAndStableErrors()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddLocalization();
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        builder.Services.AddRateLimitingPolicies();
        builder.Services.AddExceptionHandler<AppExceptionHandler>();
        builder.Services.AddSingleton(A.Fake<IRequestEmailVerificationUseCase>());
        builder.Services.AddSingleton(A.Fake<IRequestPasswordRecoveryUseCase>());
        var confirmation = A.Fake<IConfirmEmailVerificationUseCase>();
        A.CallTo(() => confirmation.Handler(A<ei_back.Core.Application.UseCase.User.Dtos.ConfirmEmailVerificationRequestDto>._, A<CancellationToken>._))
            .Returns(CodeConfirmationResult.Confirmed);
        builder.Services.AddSingleton(confirmation);
        var recovery = A.Fake<IConfirmPasswordRecoveryUseCase>();
        A.CallTo(() => recovery.Handler(A<ei_back.Core.Application.UseCase.User.Dtos.ConfirmPasswordRecoveryRequestDto>._, A<CancellationToken>._))
            .Returns(CodeConfirmationResult.Invalid);
        builder.Services.AddSingleton(recovery);
        var signin = A.Fake<ISignInUseCase>();
        A.CallTo(() => signin.Handler(A<ei_back.Core.Application.UseCase.User.Dtos.LoginDtoRequest>._))
            .Throws(new EmailVerificationRequiredException());
        builder.Services.AddSingleton(signin);
        builder.Services.AddSingleton(A.Fake<IRefreshTokenUseCase>());
        builder.Services.AddSingleton(A.Fake<ILogoutUseCase>());
        builder.Services.AddSingleton(A.Fake<IChangePasswordUseCase>());
        builder.Services.AddSingleton(A.Fake<IGetUserNameUseCase>());
        builder.Services.AddSingleton(A.Fake<IUnitOfWork>());
        await using var app = builder.Build();
        app.UseExceptionHandler(_ => { });
        app.UseRouting();
        app.UseRateLimiter();
        app.MapControllers();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        foreach (var route in new[] { "email-verification", "password-recovery" })
        {
            var result = await client.PostAsJsonAsync("/api/user/auth/" + route, new { email = "test@example.com" });
            result.StatusCode.Should().Be(HttpStatusCode.Accepted);
            (await result.Content.ReadAsStringAsync()).Should().BeEmpty();
        }
        var confirmed = await client.PostAsJsonAsync("/api/user/auth/email-verification/confirm", new { email = "test@example.com", code = "000123" });
        confirmed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await confirmed.Content.ReadAsStringAsync()).Should().BeEmpty();
        var invalid = await client.PostAsJsonAsync("/api/user/auth/password-recovery/confirm", new { email = "test@example.com", code = "000123", newPassword = "new-password" });
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
        error.RootElement.GetProperty("code").GetString().Should().Be("invalid_verification_code");
        error.RootElement.GetProperty("statusCode").GetInt32().Should().Be(400);
        var badPayload = await client.PostAsJsonAsync("/api/user/auth/password-recovery/confirm", new { email = "test@example.com", code = "12345x", newPassword = "a" });
        badPayload.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        JsonDocument.Parse(await badPayload.Content.ReadAsStringAsync()).RootElement.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Object);
        var login = await client.PostAsJsonAsync("/api/user/auth/signin", new { userName = "tester", password = "password" });
        login.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement.GetProperty("code").GetString().Should().Be("email_verification_required");
        await app.StopAsync();
    }
}