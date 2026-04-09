using ChatSpark.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatSpark.Infrastructure.Persistence.Configurations
{
    public class ChannelConfiguration :IEntityTypeConfiguration<Channel>
    {
        public void Configure(EntityTypeBuilder<Channel> builder)
        {
            builder.ToTable("channels");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
            builder.Property(c => c.WorkspaceId).IsRequired();

            builder.HasIndex(c => new { c.WorkspaceId, c.Name }).IsUnique();

            builder.Property(c => c.CreatedAt).IsRequired().HasColumnType("timestampz");

            builder.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(c => c.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
