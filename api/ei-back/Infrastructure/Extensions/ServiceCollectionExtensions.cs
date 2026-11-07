using Resend;
using ei_back.Core.Application.Service.Email;
using ei_back.Core.Application.Service.Verification;
using ei_back.Core.Application.Service.Verification.Interfaces;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Infrastructure.Verification;
using ei_back.Infrastructure.Email;
using ei_back.Core.Application.Interfaces;
using ei_back.Core.Application.Service.Encryption;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.Service.Play;
using ei_back.Core.Application.Service.Play.Interfaces;
using ei_back.Core.Application.UseCase.Game;
using ei_back.Core.Application.UseCase.Game.Interfaces;
using ei_back.Core.Application.UseCase.GameInfo;
using ei_back.Core.Application.UseCase.GameInfo.Interfaces;
using ei_back.Core.Application.UseCase.Play;
using ei_back.Core.Application.UseCase.Play.Interfaces;
using ei_back.Core.Application.UseCase.Role;
using ei_back.Core.Application.UseCase.Role.Interfaces;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Infrastructure.Context;
using ei_back.Core.Application.Repository;
using ei_back.Infrastructure.Context.Repository;
using ei_back.Infrastructure.GenAI;
using ei_back.Infrastructure.Token;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using System.Threading.RateLimiting;

namespace ei_back.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();

            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            services.AddScoped<IGameRepository, GameRepository>();
            services.AddScoped<IPlayRepository, PlayRepository>();
            services.AddScoped<IGameInfoRepository, GameInfoRepository>();

            return services;
        }

        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddOptions<VerificationCodeOptions>()
                .BindConfiguration("VerificationCode")
                .Validate(o => System.Text.Encoding.UTF8.GetByteCount(o.HashKey) >= 32,
                    "VerificationCode:HashKey must contain at least 32 UTF-8 bytes.");
            services.AddScoped<IVerificationCodeService, VerificationCodeService>();
            services.AddScoped<IVerificationFlowService, VerificationFlowService>();
            services.AddScoped<IVerificationCodeProtector, VerificationCodeProtector>();
            services.AddOptions<ResendClientOptions>()
                .BindConfiguration("Resend")
                .Validate(o => !string.IsNullOrWhiteSpace(o.ApiToken), "Resend:ApiToken is required.");
            services.AddOptions<ResendEmailOptions>()
                .BindConfiguration("Resend")
                .Validate(o => System.Net.Mail.MailAddress.TryCreate(o.From, out _), "Resend:From must be a valid sender email address.");
            services.AddResend(_ => { });
            services.AddScoped<IVerificationEmailComposer, VerificationEmailComposer>();
            services.AddScoped<IEmailSender, ResendEmailSender>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEncryptionService, EncryptionService>();
            services.AddScoped<IGeneratePlaysSummaryService, GeneratePlaysSummaryService>();
            services.AddScoped<IInitialMasterPlayService, InitialMasterPlayService>();
            services.AddScoped<IUpsertWorldInfoService, UpsertWorldInfoService>();
            services.AddScoped<IPlayAnalyzerService, PlayAnalyzerService>();

            return services;
        }

        public static IServiceCollection AddUseCases(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IRequestPasswordRecoveryUseCase, RequestPasswordRecoveryUseCase>();
            services.AddScoped<IConfirmPasswordRecoveryUseCase, ConfirmPasswordRecoveryUseCase>();
            services.AddScoped<IRequestEmailVerificationUseCase, RequestEmailVerificationUseCase>();
            services.AddScoped<IConfirmEmailVerificationUseCase, ConfirmEmailVerificationUseCase>();
            services.AddScoped<ICreateUserUseCase, CreateUserUseCase>();
            services.AddScoped<IGetUserUseCase, GetUserUseCase>();
            services.AddScoped<ICheckUserInfoUseCase, CheckUserInfoUseCase>();
            services.AddScoped<ISignInUseCase, SigninUseCase>();
            services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
            services.AddScoped<ILogoutUseCase, LogoutUseCase>();
            services.AddScoped<IChangePasswordUseCase, ChangePasswordUseCase>();
            services.AddScoped<IDeleteUserUseCase, DeleteUserUseCase>();
            services.AddScoped<IGetUserNameUseCase, GetUserNameUseCase>();
            services.AddScoped<IApplyRolesUseCase, ApplyRolesUseCase>();
            services.AddScoped<IGetAllRoleUseCase, GetAllRoleUseCase>();
            services.AddScoped<ICreateGameUseCase, CreateGameUseCase>();
            services.AddScoped<IGetGamesUseCase, GetGamesUseCase>();
            services.AddScoped<IGetPlaysUseCase, GetPlaysUseCase>();

            var bypassLlm = configuration.GetValue<bool>("Features:BypassLlmGenerator");
            if (bypassLlm)
                services.AddScoped<INewUserPlayUseCase, BypassNewUserPlayUseCase>();
            else
                services.AddScoped<INewUserPlayUseCase, NewUserPlayUseCase>();

            services.AddScoped<ICreateGameInfoUseCase, CreateGameInfoUseCase>();
            services.AddScoped<IGetGameByIdAndUserUseCase, GetGameByIdAndUserUseCase>();
            services.AddScoped<IDeleteGameUseCase, DeleteGameUseCase>();
            services.AddScoped<IUpdateGameUseCase, UpdateGameUseCase>();

            return services;
        }

        public static IServiceCollection AddInfraHttpClients(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddGenAiClient(this IServiceCollection services)
        {
            services.AddScoped<IGenAi, GenAi>();
            return services;
        }

        public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = 429;
                options.OnRejected = (context, cancellationToken) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        context.HttpContext.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
                            .ToString(System.Globalization.CultureInfo.InvariantCulture);
                    return ValueTask.CompletedTask;
                };

                options.AddPolicy("VerificationRequest", context =>
                    RateLimitPartition.GetSlidingWindowLimiter(GetClientIp(context), _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 5, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6, QueueLimit = 0
                    }));
                options.AddPolicy("VerificationConfirm", context =>
                    RateLimitPartition.GetSlidingWindowLimiter(GetClientIp(context), _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 10, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6, QueueLimit = 0
                    }));

                options.AddPolicy("Login", context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetClientIp(context),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueLimit = 0
                        }));

                options.AddPolicy("Signup", context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetClientIp(context),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 3,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueLimit = 0
                        }));

                options.AddPolicy("Refresh", context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetClientIp(context),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueLimit = 0
                        }));

                options.AddPolicy("UsernameCheck", context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetClientIp(context),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueLimit = 0
                        }));

                options.AddPolicy("Authenticated", context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetClientIp(context),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 120,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueLimit = 0
                        }));
            });

            return services;
        }

        private static string GetClientIp(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString()
                   ?? "unknown";
        }
    }
}
