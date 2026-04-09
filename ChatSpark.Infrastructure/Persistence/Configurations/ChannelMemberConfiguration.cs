using ChatSpark.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace ChatSpark.Infrastructure.Persistence.Configurations
{
    public class ChannelMemberConfiguration : IEntityTypeConfiguration<ChannelMember>
    {
        
        public void Configure(EntityTypeBuilder<ChannelMember> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.ChannelId).IsRequired();
            builder.Property(c => c.UserId).IsRequired();

            builder.HasIndex(c => new { c.ChannelId, c.UserId }).IsUnique();

            builder.HasIndex(c => c.UserId);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Channel>()
                .WithMany()
                .HasForeignKey(c => c.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);



        }
    }
}
