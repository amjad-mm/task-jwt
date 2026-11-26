using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Models;

namespace TaskManager.Services;

public sealed class InMemoryUserStore
{
    private readonly ConcurrentDictionary<string, AppUser> _users =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public InMemoryUserStore(IConfiguration configuration, ILogger<InMemoryUserStore> logger)
    {
        var adminEmail = configuration["Admin:Email"];
        var adminPassword = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(adminEmail) && string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("No admin account configured. Set Admin__Email and Admin__Password to seed one.");
            return;
        }

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException("Both Admin:Email and Admin:Password must be configured together.");
        }

        var admin = new AppUser
        {
            Email = adminEmail.Trim(),
            PasswordHash = "",
            Role = "Admin"
        };
        admin.PasswordHash = _passwordHasher.HashPassword(admin, adminPassword);
        _users.TryAdd(admin.Email, admin);
    }

    public AppUser? FindByEmail(string email) =>
        _users.TryGetValue(email.Trim(), out var user) ? user : null;

    public bool TryCreate(string email, string password, out AppUser? user)
    {
        var newUser = new AppUser
        {
            Email = email.Trim(),
            PasswordHash = "",
            Role = "User"
        };
        newUser.PasswordHash = _passwordHasher.HashPassword(newUser, password);
        var added = _users.TryAdd(newUser.Email, newUser);
        user = added ? newUser : null;
        return added;
    }

    public bool VerifyPassword(AppUser user, string password) =>
        _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password)
        != PasswordVerificationResult.Failed;
}

public sealed class AuthService
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);
    private readonly ConcurrentDictionary<string, RefreshSession> _refreshTokens = new();
    private readonly InMemoryUserStore _users;
    private readonly string _signingKey;

    public AuthService(InMemoryUserStore users, IConfiguration configuration)
    {
        _users = users;
        _signingKey = configuration["Jwt:Key"]
            ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public AppUser? Register(string email, string password) =>
        _users.TryCreate(email, password, out var user) ? user : null;

    public AppUser? Login(string email, string password)
    {
        var user = _users.FindByEmail(email);
        return user is not null && _users.VerifyPassword(user, password) ? user : null;
    }

    public TokenResponse CreateTokens(AppUser user)
    {
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "TaskManager",
            audience: "TaskManagerClient",
            claims: claims,
            notBefore: now,
            expires: now.Add(AccessTokenLifetime),
            signingCredentials: credentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        _refreshTokens[refreshToken] = new RefreshSession(user, now.Add(RefreshTokenLifetime));

        return new TokenResponse(
            accessToken,
            refreshToken,
            (int)AccessTokenLifetime.TotalSeconds,
            new UserSummary(user.Email, user.Role));
    }

    public TokenResponse? Refresh(string refreshToken)
    {
        if (!_refreshTokens.TryRemove(refreshToken, out var session)
            || session.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        return CreateTokens(session.User);
    }

    private sealed record RefreshSession(AppUser User, DateTime ExpiresAt);
}

public sealed class TaskService
{
    private readonly ConcurrentDictionary<Guid, WorkTask> _tasks = new();

    public PagedTasks GetTasks(int page, int pageSize, string? search)
    {
        var filtered = _tasks.Values
            .Where(task => string.IsNullOrWhiteSpace(search)
                || task.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                || task.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(task => task.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();
        var totalPages = (int)Math.Ceiling(filtered.Length / (double)pageSize);
        return new PagedTasks(items, page, pageSize, filtered.Length, totalPages);
    }

    public WorkTask? Find(Guid id) =>
        _tasks.TryGetValue(id, out var task) ? task : null;

    public WorkTask Create(string title, string description, string status)
    {
        var task = new WorkTask
        {
            Title = title.Trim(),
            Description = description.Trim(),
            Status = status
        };
        _tasks[task.Id] = task;
        return task;
    }

    public WorkTask? Update(Guid id, string title, string description, string status)
    {
        if (!_tasks.TryGetValue(id, out var task))
        {
            return null;
        }

        task.Title = title.Trim();
        task.Description = description.Trim();
        task.Status = status;
        return task;
    }

    public bool Delete(Guid id) => _tasks.TryRemove(id, out _);
}
