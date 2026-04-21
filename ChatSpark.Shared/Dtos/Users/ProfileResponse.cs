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
}
