namespace ChatSpark.Shared.Dtos.Users
{
    public record ProfileResponse(
        Guid Id,
        string Email,
        string DisplayName,
        string? AvatarUrl,
        string? Bio,
        string? WebsiteUrl,
        DateTime CreatedAt);

    // Public view — no email exposed
    public record PublicProfileResponse(
        Guid Id,
        string DisplayName,
        string? AvatarUrl,
        string? Bio,
        string? WebsiteUrl,
        DateTime CreatedAt);
}
