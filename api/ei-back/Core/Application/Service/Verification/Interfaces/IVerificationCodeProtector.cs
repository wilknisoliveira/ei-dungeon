using ei_back.Core.Domain.Entity;

namespace ei_back.Core.Application.Service.Verification.Interfaces;

public interface IVerificationCodeProtector
{
    string GenerateCode();
    string Hash(VerificationCode record, string code);
    bool Matches(VerificationCode record, string code);
}