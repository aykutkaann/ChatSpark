namespace ChatSpark.Shared.Dtos.Workspaces
{
    public record WorkspaceMemberResponse(
        Guid UserId,
        string DisplayName,
        string? AvatarUrl,
        string Role,
        bool IsOnline);
}
