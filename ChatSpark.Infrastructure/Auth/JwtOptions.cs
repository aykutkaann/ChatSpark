using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatSpark.Infrastructure.Auth
{
    public class JwtOptions 
    {
        public const string SectionName = "Jwt";

        public string SigningKey { get; init; } = string.Empty;
        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public int ExpiryMinutes { get; init; }
        public int RefreshTokenExpiryDays { get; init; } 
    }
}
