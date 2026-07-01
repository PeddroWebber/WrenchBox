namespace WrenchBox.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, string email, string role);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
