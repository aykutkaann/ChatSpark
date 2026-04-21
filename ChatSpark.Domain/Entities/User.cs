
namespace ChatSpark.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; } = null!;
        public string DisplayName { get; private set; } = null!;
        public string PasswordHash { get; private set; } = null!;
        public string? AvatarUrl { get; private set; }
        public string? Bio { get; private set; }
        public string? WebsiteUrl { get; private set; }
        public DateTime CreatedAt { get; private set; }


        private User() { }

        public static  User Create(string email, string displayName, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                throw new ArgumentException("Invalid or empty email address.");


            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name cannot be empty.");

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password cannot be empty.");

            return new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = displayName,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };
        }

        public  void  UpdateDisplayName(string newName)
        {

            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Display name cannot be empty.");

            DisplayName = newName;

        }

        public void UpdateAvatar(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Url cannot be empty.");
            AvatarUrl = url;
        }

        public void ChangePassword(string newHash)
        {
            if (string.IsNullOrWhiteSpace(newHash))
                throw new ArgumentException("New hashed password cannot be empty.");

            PasswordHash = newHash;
        }

        public void UpdateProfile(string? bio, string? websiteUrl)
        {
            if (bio is not null && bio.Length > 300)
                throw new ArgumentException("Bio cannot exceed 300 characters.");

            if (websiteUrl is not null && websiteUrl.Length > 512)
                throw new ArgumentException("Website URL is too long.");

            Bio = bio;
            WebsiteUrl = websiteUrl;
        }
    }
}
