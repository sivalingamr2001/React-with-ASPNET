namespace JanaticsApi.Domain.Services;

public interface IPasswordHashingService
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
