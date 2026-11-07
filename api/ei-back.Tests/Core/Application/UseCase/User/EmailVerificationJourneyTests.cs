using ei_back.Infrastructure.Context;
using AutoMapper;
using ei_back.Core.Application.Service.Encryption;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Infrastructure.Context.Repository;
using ei_back.Infrastructure.Mappings;
using ei_back.Infrastructure.Token;
using ei_back.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ei_back.Tests.Core.Application.UseCase.User;

public class EmailVerificationJourneyTests
{
    [VerificationDatabaseFact]
    public async Task ConcurrentRegistration_UsesUniqueIndexWithoutExplicitTransaction_AndCreatesOnlyOneCode()
    {
        var username = "race" + Guid.NewGuid().ToString("N")[..12];
        var email = username + "@example.com";
        var mapper = new MapperConfiguration(c => c.AddProfile<MappingsProfile>()).CreateMapper();
        var delivered = 0;
        var sender = VerificationTestSupport.Capture((_, _) => Interlocked.Increment(ref delivered));
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ =>
        {
            await using var db = VerificationTestSupport.Open();
            var create = new CreateUserUseCase(new UserRepository(db), mapper,
                new UnitOfWork(db), VerificationTestSupport.Flow(db, sender));
            try
            {
                await create.Handler(new UserDtoRequest { UserName = username, FullName = "Race Test", Email = email, Password = "password123" });
                return true;
            }
            catch (RegistrationConflictException) { return false; }
            catch (ei_back.Infrastructure.Exceptions.ExceptionTypes.BadRequestException) { return false; }
        }));
        results.Count(x => x).Should().Be(1);
        delivered.Should().Be(1);
        await using var check = VerificationTestSupport.Open();
        (await check.Users.CountAsync(x => x.UserName == username)).Should().Be(1);
        (await check.VerificationCodes.CountAsync(x => x.Email == email.ToUpperInvariant())).Should().Be(1);
    }
    [VerificationDatabaseFact]
    public async Task Registration_Confirmation_AndLogin_WorkWithCommittedCodes()
    {
        var username = "verify" + Guid.NewGuid().ToString("N")[..10];
        var email = username + "@example.com";
        string plain = "";
        var sender = VerificationTestSupport.Capture((_, code) => plain = code);
        var mapper = new MapperConfiguration(c => c.AddProfile<MappingsProfile>()).CreateMapper();
        await using (var db = VerificationTestSupport.Open())
        {
            var create = new CreateUserUseCase(new UserRepository(db), mapper,
                new UnitOfWork(db), VerificationTestSupport.Flow(db, sender));
            var response = await create.Handler(new UserDtoRequest { UserName = username, FullName = "Test", Email = email, Password = "old-password" });
            response.Email.Should().Be(email);
        }
        await using (var db = VerificationTestSupport.Open())
        {
            var user = await db.Users.SingleAsync(x => x.UserName == username);
            user.EmailVerifiedAt.Should().BeNull();
            user.NormalizedEmail.Should().Be(email.ToUpperInvariant());
            var tokens = A.Fake<ITokenService>();
            var signin = new SigninUseCase(new TokenConfiguration { Minutes = 30, DaysToExpiry = 7 },
                new UserRepository(db), tokens, new EncryptionService(), new RefreshTokenRepository(db));
            Action attempt = () => signin.Handler(new() { UserName = username, Password = "old-password" });
            attempt.Should().Throw<EmailVerificationRequiredException>();
            A.CallTo(() => tokens.GenerateAccessToken(A<IEnumerable<Claim>>._)).MustNotHaveHappened();
        }
        await using (var db = VerificationTestSupport.Open())
        {
            var confirm = new ConfirmEmailVerificationUseCase(VerificationTestSupport.Flow(db, sender));
            (await confirm.Handler(new() { Email = email, Code = "bad" })).Should().Be(CodeConfirmationResult.Invalid);
        }
        await using (var db = VerificationTestSupport.Open())
        {
            (await db.VerificationCodes.SingleAsync(x => x.Email == email.ToUpperInvariant())).FailedAttempts.Should().Be(1);
            var confirm = new ConfirmEmailVerificationUseCase(VerificationTestSupport.Flow(db, sender));
            (await confirm.Handler(new() { Email = email.ToUpperInvariant(), Code = plain })).Should().Be(CodeConfirmationResult.Confirmed);
        }
        await using (var db = VerificationTestSupport.Open())
        {
            var tokens = A.Fake<ITokenService>();
            A.CallTo(() => tokens.GenerateAccessToken(A<IEnumerable<Claim>>._)).Returns("access");
            A.CallTo(() => tokens.GenerateRefreshToken()).Returns("refresh");
            var signin = new SigninUseCase(new TokenConfiguration { Minutes = 30, DaysToExpiry = 7 },
                new UserRepository(db), tokens, new EncryptionService(), new RefreshTokenRepository(db));
            signin.Handler(new() { UserName = username, Password = "old-password" }).AccessToken.Should().Be("access");
        }
    }
}