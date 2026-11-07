using ei_back.Core.Application.Service.Encryption.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace ei_back.Core.Application.Service.Encryption
{
    public class EncryptionService : IEncryptionService
    {
        public string ComputeSha256(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashedBytes = SHA256.HashData(inputBytes);
            return BitConverter.ToString(hashedBytes);
        }

        public string ComputeSha256Hash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        public string ComputeBcryptHash(string input)
        {
            return BCrypt.Net.BCrypt.HashPassword(input);
        }

        public bool VerifyBcryptHash(string input, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(input, hash);
        }
    }
}
