
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace BoidsAuth.Security;

public sealed record LocalUser(
    string Username,
    string PasswordHash,
    string Role);

public sealed class LocalUserStore
{
    private readonly string _filePath;
    private readonly IPasswordHasher<LocalUser> _hasher;
    private readonly List<LocalUser> _users;
    private readonly object _sync = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public LocalUserStore(
        string filePath,
        IPasswordHasher<LocalUser> hasher)
    {
        _filePath = filePath;
        _hasher = hasher;

        Directory.CreateDirectory(
            Path.GetDirectoryName(_filePath)!);

        if (File.Exists(_filePath))
        {
            _users = JsonSerializer.Deserialize<List<LocalUser>>(
                File.ReadAllText(_filePath))
                ?? throw new InvalidDataException(
                    "The local user store contains no user list.");
        }
        else
        {
            _users = SeedUsers();
            Save();
        }
    }

    public LocalUser? ValidateCredentials(
        string username,
        string password)
    {
        lock (_sync)
        {
            LocalUser? user = _users.FirstOrDefault(
                candidate => string.Equals(
                    candidate.Username,
                    username,
                    StringComparison.OrdinalIgnoreCase));

            if (user is null)
            {
                return null;
            }

            PasswordVerificationResult result =
                _hasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    password);

            return result == PasswordVerificationResult.Failed
                ? null
                : user;
        }
    }

    private List<LocalUser> SeedUsers()
    {
        LocalUser admin = new("admin", "", "Administrator");
        LocalUser viewer = new("viewer", "", "Viewer");

        return
        [
            admin with
            {
                PasswordHash = _hasher.HashPassword(
                    admin,
                    "BoidsDemo!2026")
            },
            viewer with
            {
                PasswordHash = _hasher.HashPassword(
                    viewer,
                    "ViewerDemo!2026")
            }
        ];
    }

    private void Save()
    {
        File.WriteAllText(
            _filePath,
            JsonSerializer.Serialize(_users, JsonOptions));
    }
}
