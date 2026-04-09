using ChatSpark.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatSpark.Infrastructure.Persistence.Configurations
{
    public class WorkspaceMemberConfiguration :IEntityTypeConfiguration<WorkspaceMember>
    {
        public  void Configure(EntityTypeBuilder<WorkspaceMember> builder)
        {
            builder.HasKey(w => w.Id);

            builder.Property(w => w.UserId).IsRequired();
            builder.Property(w => w.WorkspaceId).IsRequired();

            builder.Property(w => w.Role).HasConversion<int>();

            builder.HasIndex(w => new {w.WorkspaceId, w.UserId}).IsUnique();

            builder.HasIndex(w => w.UserId);

            builder.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(w => w.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

       
                




        }
    }
}
