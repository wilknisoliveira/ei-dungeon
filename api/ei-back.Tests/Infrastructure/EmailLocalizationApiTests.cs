using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Email;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.Service.Verification;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Extensions;
using ei_back.UserInterface.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace ei_back.Tests.Infrastructure;

public class EmailLocalizationApiTests
{
    [Theory]
    [InlineData(null, "en", "Verify", "Password recovery")]
    [InlineData("en", "en", "Verify", "Password recovery")]
    [InlineData("pt-BR", "pt-BR", "Confirme", "Recuperação")]
    [InlineData("es", "es", "Verifica", "Recuperación")]
    [InlineData("en-US", "en", "Verify", "Password recovery")]
    [InlineData("es-ES", "es", "Verifica", "Recuperación")]
    [InlineData("de", "en", "Verify", "Password recovery")]
    [InlineData("fr;q=1,es;q=0.8,en;q=0.5", "es", "Verifica", "Recuperación")]
    public async Task AllEmailEntryPoints_UseRequestLanguage_WithEnglishFallback(string? header, string language, string verificationSubject, string recoverySubject)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddApplicationLocalization();
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        builder.Services.AddRateLimitingPolicies();
        builder.Services.AddTransient<IVerificationEmailComposer, VerificationEmailComposer>();
        OutgoingEmail? captured = null;
        string? culture = null;
        IVerificationEmailComposer composer = null!;
        void Capture(VerificationCodePurpose purpose)
        {
            culture = CultureInfo.CurrentUICulture.Name;
            captured = composer.Compose(new CodeDelivery("player@example.com", "000123", purpose, TimeSpan.FromMinutes(10)));
        }
        var create = A.Fake<ICreateUserUseCase>();
        A.CallTo(() => create.Handler(A<UserDtoRequest>._)).ReturnsLazily((UserDtoRequest request) =>
        {
            Capture(VerificationCodePurpose.EmailVerification);
            return Task.FromResult(new UserDtoResponse { UserName = request.UserName, FullName = request.FullName, Email = request.Email });
        });
        var verify = A.Fake<IRequestEmailVerificationUseCase>();
        A.CallTo(() => verify.Handler(A<EmailCodeRequestDto>._, A<CancellationToken>._)).Invokes(() => Capture(VerificationCodePurpose.EmailVerification));
        var recover = A.Fake<IRequestPasswordRecoveryUseCase>();
        A.CallTo(() => recover.Handler(A<EmailCodeRequestDto>._, A<CancellationToken>._)).Invokes(() => Capture(VerificationCodePurpose.PasswordRecovery));
        builder.Services.AddSingleton(create);
        builder.Services.AddSingleton(verify);
        builder.Services.AddSingleton(recover);
        builder.Services.AddSingleton(A.Fake<IConfirmEmailVerificationUseCase>());
        builder.Services.AddSingleton(A.Fake<IConfirmPasswordRecoveryUseCase>());
        builder.Services.AddSingleton(A.Fake<ISignInUseCase>());
        builder.Services.AddSingleton(A.Fake<IRefreshTokenUseCase>());
        builder.Services.AddSingleton(A.Fake<ILogoutUseCase>());
        builder.Services.AddSingleton(A.Fake<IChangePasswordUseCase>());
        builder.Services.AddSingleton(A.Fake<IGetUserNameUseCase>());
        builder.Services.AddSingleton(A.Fake<IGetUserUseCase>());
        builder.Services.AddSingleton(A.Fake<ICheckUserInfoUseCase>());
        builder.Services.AddSingleton(A.Fake<IDeleteUserUseCase>());
        builder.Services.AddSingleton(A.Fake<IUnitOfWork>());
        await using var app = builder.Build();
        composer = app.Services.GetRequiredService<IVerificationEmailComposer>();
        app.UseRequestLocalization();
        app.UseRouting();
        app.UseRateLimiter();
        app.MapControllers();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        if (header != null) client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", header);
        var signup = await client.PostAsJsonAsync("/api/user", new { userName = "player", fullName = "Player Name", email = "player@example.com", password = "password123" });
        signup.StatusCode.Should().Be(HttpStatusCode.OK);
        culture.Should().Be(language);
        captured!.Subject.Should().Contain(verificationSubject);
        captured.HtmlBody.Should().Contain($"lang=\"{language}\"");
        foreach (var route in new[] { "email-verification", "password-recovery" })
        {
            var response = await client.PostAsJsonAsync("/api/user/auth/" + route, new { email = "player@example.com" });
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            captured!.Subject.Should().Contain(route == "email-verification" ? verificationSubject : recoverySubject);
            captured.HtmlBody.Should().Contain($"lang=\"{language}\"");
            culture.Should().Be(language);
        }
        app.Services.GetRequiredService<IStringLocalizer<AuthController>>()["AuthInvalidRequest"].ResourceNotFound.Should().BeFalse();
        await app.StopAsync();
    }
}
