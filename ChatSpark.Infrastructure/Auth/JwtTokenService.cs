using ChatSpark.Application.Abstractions;
using ChatSpark.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ChatSpark.Infrastructure.Auth
{
    public class JwtTokenService : ITokenService
    {
        private readonly JwtOptions _options;

        public JwtTokenService(IOptions<JwtOptions> jwtOptions)
        {
            _options = jwtOptions.Value;
        }

        public (string Token, DateTime ExpiresAt) GenerateAccessToken(User user)
        {
            var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Name, user.DisplayName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return (jwt, expires);
        }

        public (string RawToken, string TokenHash, DateTime ExpiresAt) GenerateRefreshToken()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
            string rawToken = Convert.ToBase64String(randomBytes);
            string tokenHash = HashRefreshToken(rawToken);
            DateTime expiry = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);

            return (rawToken, tokenHash, expiry);
        }

        public string HashRefreshToken(string rawToken)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(rawToken);
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
