using ChatSpark.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatSpark.Infrastructure.Persistence.Configurations
{
    public class ChannelConfiguration :IEntityTypeConfiguration<Channel>
    {
        public void Configure(EntityTypeBuilder<Channel> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
            builder.Property(c => c.WorkspaceId).IsRequired();

            builder.HasIndex(c => new { c.WorkspaceId, c.Name }).IsUnique();

            builder.Property(c => c.CreatedAt).IsRequired();
            builder.Property(c => c.IsArchived).IsRequired();

            builder.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(c => c.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
