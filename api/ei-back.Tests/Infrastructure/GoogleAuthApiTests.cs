using System.Net;
using System.Net.Http.Json;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Application.UseCase.GoogleAuth.Interfaces;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions;
using ei_back.Infrastructure.Extensions;
using ei_back.UserInterface.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ei_back.Infrastructure.Token;
using System.Security.Claims;

namespace ei_back.Tests.Infrastructure;

public class GoogleAuthApiTests
{
    [Fact]
    public async Task CorsCookiesAndLegacySubjects_AreEnforcedByActualMiddleware()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.Services.AddLocalization();
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins("http://localhost:4200").AllowAnyMethod().WithHeaders("Content-Type").AllowCredentials()));
        builder.Services.AddControllers().AddApplicationPart(typeof(GoogleAuthController).Assembly)
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
        builder.Services.AddRateLimitingPolicies();
        builder.Services.AddExceptionHandler<AppExceptionHandler>();
        builder.Services.AddSingleton<IGetUserIdUseCase, GetUserIdUseCase>();
        var issue = A.Fake<IIssueGoogleChallengeUseCase>();
        var signin = A.Fake<IGoogleSigninUseCase>();
        var link = A.Fake<ILinkGoogleUseCase>();
        var createPassword = A.Fake<ICreatePasswordUseCase>();
        var account = A.Fake<IGetCurrentAccountUseCase>();
        var userId = Guid.NewGuid();
        var challengeId = Guid.NewGuid();
        var challenge = new GoogleChallengeDtoResponse(challengeId, "nonce", DateTimeOffset.UtcNow.AddMinutes(5));
        A.CallTo(() => issue.Handler(GoogleAuthPurpose.SignIn, null, A<string>._, A<CancellationToken>._)).Returns(challenge);
        A.CallTo(() => account.Handler(userId, A<CancellationToken>._))
            .Returns(new CurrentAccountDtoResponse(userId, "player@example.com", "CommonUser", false, true));
        A.CallTo(() => signin.Handler(A<GoogleAuthRequestDto>._, A<string>._, A<CancellationToken>._))
            .Throws(new GoogleAuthenticationException("google_registration_required"));
        A.CallTo(() => issue.Handler(GoogleAuthPurpose.Link, userId, A<string>._, A<CancellationToken>._)).Returns(challenge);
        builder.Services.AddSingleton(issue);
        builder.Services.AddSingleton(signin);
        builder.Services.AddSingleton(link);
        builder.Services.AddSingleton(createPassword);
        builder.Services.AddSingleton(account);
        var tokenConfig = new TokenConfiguration { Secret = new string('s', 64), Issuer = "test", Audience = "test", Minutes = 15 };
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = tokenConfig.Issuer,
                ValidateAudience = true, ValidAudience = tokenConfig.Audience,
                ValidateLifetime = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenConfig.Secret))
            });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseExceptionHandler(_ => { });
        app.UseRouting();
        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        (await client.GetAsync("/api/user/auth/account")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization = new("Bearer", new TokenService(tokenConfig).GenerateAccessToken(new[]
        {
            new Claim("sub", userId.ToString()), new Claim("unique_name", "removed-username"), new Claim("role", "CommonUser")
        }));
        (await client.GetFromJsonAsync<CurrentAccountDtoResponse>("/api/user/auth/account"))!.Id.Should().Be(userId);
        foreach (var origin in new[] { "https://untrusted.example", "http://localhost:4200" })
        {
            using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/user/auth/google/challenge");
            preflight.Headers.Add("Origin", origin);
            preflight.Headers.Add("Access-Control-Request-Method", "POST");
            preflight.Headers.Add("Access-Control-Request-Headers", "content-type");
            using var response = await client.SendAsync(preflight);
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            if (origin == "http://localhost:4200")
                response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be(origin);
            else
                response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        }
        A.CallTo(() => issue.Handler(A<GoogleAuthPurpose>._, A<Guid?>._, A<string>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        // Non-browser clients do not need an Origin header. Browser access is governed by CORS.
        var issued = await client.PostAsJsonAsync("/api/user/auth/google/challenge", new { purpose = "SignIn" });
        issued.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookie = issued.Headers.GetValues("Set-Cookie").Single();
        cookie.Should().Contain("httponly").And.Contain("samesite=strict").And.Contain("path=/api/user/auth");
        client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        var login = await client.PostAsJsonAsync("/api/user/auth/google", new { challengeId, credential = "provider-proof" });
        login.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await login.Content.ReadAsStringAsync()).Should().Contain("google_registration_required");
        A.CallTo(() => signin.Handler(A<GoogleAuthRequestDto>._, A<string>.That.Matches(value => value.Length == 64), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        var missingAuthentication = await client.PostAsJsonAsync("/api/user/auth/google/challenge", new { purpose = "Link" });
        missingAuthentication.StatusCode.Should().Be(HttpStatusCode.OK);
        client.DefaultRequestHeaders.Authorization = null;
        (await client.PostAsJsonAsync("/api/user/auth/google/link", new { challengeId, credential = "proof" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
