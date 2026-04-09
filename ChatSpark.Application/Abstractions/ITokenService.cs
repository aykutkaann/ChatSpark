using ChatSpark.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatSpark.Application.Abstractions
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        (string RawToken, string TokenHash, DateTime ExpiresAt) GenerateRefreshToken();
        string HashRefreshToken(string rawToken);

    }
}
