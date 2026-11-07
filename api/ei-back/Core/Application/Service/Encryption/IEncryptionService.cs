namespace ei_back.Core.Application.Service.Encryption.Interfaces
{
    public interface IEncryptionService
    {
        string ComputeSha256(string input);
        string ComputeSha256Hash(string input);
        string ComputeBcryptHash(string input);
        bool VerifyBcryptHash(string input, string hash);
    }
}
