

namespace ChatSpark.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; private  set; }
        public Guid UserId { get; private set; }
        public string TokenHash { get; private set; } = null!;
        public DateTime ExpiresAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public string? ReplacedByTokenHash { get; private set; }

        private RefreshToken() { }

        public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty.");
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new ArgumentException("TokenHash cannot be empty.");
            if (expiresAt <= DateTime.UtcNow)
                throw new ArgumentException("ExpiresAt must be in the future.");

            return new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

        }

        public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;

        public void Revoke(string? replacedByTokenHash = null)
        {
            if (RevokedAt is not null)
                throw new InvalidOperationException("Refresh token is already revoked.");

            ReplacedByTokenHash = replacedByTokenHash;
            RevokedAt = DateTime.UtcNow;

        }
    }
}
