using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models;

public sealed class WorkTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Status { get; set; }
}

public sealed class AppUser
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Email { get; init; }
    public required string PasswordHash { get; set; }
    public required string Role { get; init; }
}

public sealed class RegisterRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";

    [Required, MinLength(8), StringLength(100)]
    public string Password { get; set; } = "";
}

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";

    [Required, StringLength(100)]
    public string Password { get; set; } = "";
}

public sealed class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = "";
}

public sealed class TaskRequest
{
    [Required, StringLength(120)]
    public string Title { get; set; } = "";

    [Required, StringLength(2000)]
    public string Description { get; set; } = "";

    [Required, RegularExpression("^(Todo|InProgress|Done)$")]
    public string Status { get; set; } = "";
}

public sealed record UserSummary(string Email, string Role);

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserSummary User);

public sealed record PagedTasks(
    IReadOnlyList<WorkTask> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
