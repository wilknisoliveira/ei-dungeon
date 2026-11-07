using ei_back.Core.Application.Service.Encryption;
using ei_back.Core.Application.Service.Encryption.Interfaces;

namespace ei_back.Tests.Domain.User
{
    public class EncryptionServiceTests
    {
        private readonly IEncryptionService _encryptionService;

        public EncryptionServiceTests()
        {
            _encryptionService = new EncryptionService();
        }

        [Fact]
        public void ComputeSha256_ShouldReturnUppercaseHexWithDashes()
        {
            var result = _encryptionService.ComputeSha256("test");
            result.Should().MatchRegex("^[A-F0-9-]+$");
            result.Should().Contain("-");
        }

        [Fact]
        public void ComputeSha256Hash_ShouldReturnLowercaseHexWithoutDashes()
        {
            var result = _encryptionService.ComputeSha256Hash("test");
            result.Should().MatchRegex("^[a-f0-9]+$");
            result.Should().NotContain("-");
        }

        [Fact]
        public void ComputeBcryptHash_ShouldReturnBcryptHash()
        {
            var result = _encryptionService.ComputeBcryptHash("test");
            result.Should().StartWith("$2");
        }

        [Fact]
        public void VerifyBcryptHash_ShouldReturnTrueForValidPassword()
        {
            var hash = _encryptionService.ComputeBcryptHash("test");
            var result = _encryptionService.VerifyBcryptHash("test", hash);
            result.Should().BeTrue();
        }

        [Fact]
        public void VerifyBcryptHash_ShouldReturnFalseForInvalidPassword()
        {
            var hash = _encryptionService.ComputeBcryptHash("test");
            var result = _encryptionService.VerifyBcryptHash("wrong", hash);
            result.Should().BeFalse();
        }
    }
}
