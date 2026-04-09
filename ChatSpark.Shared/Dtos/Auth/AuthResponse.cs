

namespace ChatSpark.Shared.Dtos.Auth
{

    public record AuthResponse(string AccessToken, string RefreshToken, string DisplayName, DateTime AccessTokenExpiresAt);
}
