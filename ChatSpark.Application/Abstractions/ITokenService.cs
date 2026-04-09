using ChatSpark.Domain.Entities;

namespace ChatSpark.Application.Abstractions
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);

        (string RawToken, string TokenHash, DateTime ExpiresAt) GenerateRefreshToken();
        string HashRefreshToken(string rawToken);

    }
}
