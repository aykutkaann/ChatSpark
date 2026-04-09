using ChatSpark.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace ChatSpark.Infrastructure.Persistence
{
    public class AppDbContext: DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; init; }
        public DbSet<Workspace> Workspaces { get; init; }
        public DbSet<Message> Messages { get; init; }
        public DbSet<Channel> Channels { get; init; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }

    }
}
