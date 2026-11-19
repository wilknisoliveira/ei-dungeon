using System.Security.Cryptography;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Application.UseCase.GoogleAuth.Interfaces;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ei_back.UserInterface.Api;

[ApiController]
[Route("api/user/auth")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public class GoogleAuthController(IIssueGoogleChallengeUseCase issueGoogleChallengeUseCase,
    IGoogleSigninUseCase googleSigninUseCase, ILinkGoogleUseCase linkGoogleUseCase,
    ICreatePasswordUseCase createPasswordUseCase, IGetCurrentAccountUseCase getCurrentAccountUseCase,
    IGetUserIdUseCase getUserIdUseCase, IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>Creates a browser-bound, five-minute Google authentication attempt.</summary>
    /// <remarks>Link and CreatePassword purposes require a verified authenticated account.</remarks>
    [HttpPost("google/challenge")]
    [EnableRateLimiting("Login")]
    [ProducesResponseType(typeof(GoogleChallengeDtoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Challenge(GoogleChallengeRequestDto request, CancellationToken cancellationToken)
    {
        Guid? userId = request.Purpose == GoogleAuthPurpose.SignIn ? null : getUserIdUseCase.Handler(User);
        var browserSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var challenge = await issueGoogleChallengeUseCase.Handler(request.Purpose, userId, browserSecret, cancellationToken);
        Response.Cookies.Append(CookieName(challenge.ChallengeId), browserSecret, new CookieOptions
        {
            HttpOnly = true, Secure = !environment.IsDevelopment() || Request.IsHttps,
            SameSite = SameSiteMode.Strict, Path = "/api/user/auth", Expires = challenge.ExpiresAt,
            IsEssential = true
        });
        return Ok(challenge);
    }

    /// <summary>Signs in a linked Google identity or registers an eligible new Google user.</summary>
    /// <remarks>Returns application tokens. New users with unsupported email ownership evidence must register traditionally.</remarks>
    [HttpPost("google")]
    [EnableRateLimiting("Login")]
    [ProducesResponseType(typeof(TokenDtoReponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Signin(GoogleAuthRequestDto request, CancellationToken cancellationToken)
    {
        var result = await googleSigninUseCase.Handler(request, BrowserSecret(request.ChallengeId), cancellationToken);
        ClearCookie(request.ChallengeId);
        return Ok(result);
    }

    /// <summary>Returns current account login capabilities.</summary>
    [Authorize, HttpGet("account")]
    [EnableRateLimiting("Authenticated")]
    [ProducesResponseType(typeof(CurrentAccountDtoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Account(CancellationToken cancellationToken) =>
        Ok(await getCurrentAccountUseCase.Handler(getUserIdUseCase.Handler(User), cancellationToken));

    /// <summary>Explicitly links Google to the authenticated verified account with the same email.</summary>
    [Authorize, HttpPost("google/link")]
    [EnableRateLimiting("Authenticated")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Link(GoogleAuthRequestDto request, CancellationToken cancellationToken)
    {
        await linkGoogleUseCase.Handler(getUserIdUseCase.Handler(User), request, BrowserSecret(request.ChallengeId), cancellationToken);
        ClearCookie(request.ChallengeId);
        return NoContent();
    }

    /// <summary>Creates the first local password after a new proof of the already linked Google identity.</summary>
    [Authorize, HttpPost("password")]
    [EnableRateLimiting("Authenticated")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePassword(CreatePasswordDtoRequest request, CancellationToken cancellationToken)
    {
        await createPasswordUseCase.Handler(getUserIdUseCase.Handler(User), request, BrowserSecret(request.ChallengeId), cancellationToken);
        ClearCookie(request.ChallengeId);
        return NoContent();
    }

    private static string CookieName(Guid id) => "ei-google-" + id.ToString("N");
    private string BrowserSecret(Guid id) => Request.Cookies[CookieName(id)] ?? string.Empty;
    private void ClearCookie(Guid id) => Response.Cookies.Delete(CookieName(id), new CookieOptions { Path = "/api/user/auth" });
}
