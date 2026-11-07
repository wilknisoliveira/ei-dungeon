using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ei_back.Core.Application.Service.Verification.Interfaces;
using ei_back.Core.Domain.Entity;
using Microsoft.Extensions.Options;

namespace ei_back.Infrastructure.Verification;

public class VerificationCodeProtector(IOptions<VerificationCodeOptions> options) : IVerificationCodeProtector
{
    private readonly byte[] _key = GetKey(options);

    private static byte[] GetKey(IOptions<VerificationCodeOptions> options)
    {
        var key = Encoding.UTF8.GetBytes(options.Value.HashKey);
        if (key.Length < 32)
            throw new InvalidOperationException("VerificationCode:HashKey must contain at least 32 UTF-8 bytes. Configure it through secrets or environment variables.");
        return key;
    }

    public string GenerateCode() => RandomNumberGenerator.GetInt32(1_000_000).ToString("D6", CultureInfo.InvariantCulture);

    public string Hash(VerificationCode record, string code)
    {
        var payload = $"{record.Id:N}|{record.UserId:N}|{(int)record.Purpose}|{record.Email}|{code}";
        return Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(payload)));
    }

    public bool Matches(VerificationCode record, string code)
    {
        if (code.Length != 6 || code.Any(c => c < '0' || c > '9')) return false;
        try
        {
            return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(record.CodeHash), Convert.FromHexString(Hash(record, code)));
        }
        catch (FormatException) { return false; }
    }
}