using ChatSpark.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatSpark.Infrastructure.Persistence.Configurations
{
    public class WorkspaceConfiguration :IEntityTypeConfiguration<Workspace>
    {
        public void Configure(EntityTypeBuilder<Workspace> builder)
        {

            builder.HasKey(w => w.Id);

            builder.Property(w => w.Name).IsRequired().HasMaxLength(100);
            builder.Property(w => w.Slug).IsRequired().HasMaxLength(50);
            builder.Property(w => w.CreatedAt).IsRequired();

            builder.HasIndex(w => w.Slug).IsUnique();

            builder.Property(w => w.OwnerId).IsRequired();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(w => w.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            
        }
    }
}
