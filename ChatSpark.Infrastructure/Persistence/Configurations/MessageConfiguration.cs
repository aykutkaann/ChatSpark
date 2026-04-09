using ChatSpark.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace ChatSpark.Infrastructure.Persistence.Configurations
{
    public class MessageConfiguration :IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Content).IsRequired().HasMaxLength(4000);

            builder.Property(m => m.ChannelId).IsRequired();

            builder.Property(m => m.SenderId).IsRequired();
            builder.HasIndex(m => new { m.ChannelId, m.SentAt });


            builder.Property(m => m.SentAt).IsRequired();
            builder.Property(m => m.EditedAt).IsRequired(false);

            builder.HasOne<Channel>()
                .WithMany()
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);




        }
    }
}
