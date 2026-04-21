namespace ChatSpark.Shared.Dtos.Users
{
    public record UpdateProfileRequest(
        string? DisplayName,
        string? AvatarUrl,
        string? Bio,
        string? WebsiteUrl);
}
