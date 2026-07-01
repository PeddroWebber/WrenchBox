namespace WrenchBox.Domain.Entities;

public class AdminUser
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = "Admin";
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private AdminUser() { }

    public static AdminUser Create(string email, string passwordHash, string role = "Admin")
    {
        return new AdminUser
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role
        };
    }
}
