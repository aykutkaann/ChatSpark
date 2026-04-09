using ChatSpark.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace ChatSpark.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration :IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(512);
            builder.HasIndex(t => t.TokenHash).IsUnique();

            builder.HasIndex(t => t.UserId);
            builder.Property(t => t.UserId).IsRequired();

            builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(512);



            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
