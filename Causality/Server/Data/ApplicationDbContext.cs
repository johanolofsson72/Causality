using Causality.Shared.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Causality.Server.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Event { get; set; }
        public DbSet<Cause> Cause { get; set; }
        public DbSet<Class> Class { get; set; }
        public DbSet<Effect> Effect { get; set; }
        public DbSet<Exclude> Exclude { get; set; }
        public DbSet<Meta> Meta { get; set; }
        public DbSet<User> User { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Event>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable("Event");
                f.HasIndex(e => new { e.Id });
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<Cause>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable("Cause");
                f.HasIndex(e => new { e.Id });
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<Class>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable("Class");
                f.HasIndex(e => new { e.Id });
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<Effect>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable("Effect");
                f.HasIndex(e => new { e.Id });
                f.HasIndex(e => new { e.Id, e.UserId });
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<Exclude>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable("Exclude");
                f.HasIndex(e => new { e.Id });
                f.HasIndex(e => new { e.Id, e.UserId });
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<Meta>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable("Meta");
                f.HasIndex(e => new { e.Id });
                f.Property("Key").IsRequired();
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<User>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable("User");
                f.HasIndex(e => new { e.Id });
                f.Property("UID").IsRequired();
                f.Property("IP").IsRequired();
                f.Property("Name").IsRequired();
                f.Property("Email").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });

            base.OnModelCreating(modelBuilder);

            //modelBuilder.Entity<Setting>().HasData(
            //    new Setting { Id = 1, Key = "Setting 1", Value = "1", UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") });
            //modelBuilder.Entity<Setting>().HasData(
            //    new Setting { Id = 2, Key = "Setting 2", Value = "2", UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") });
            //modelBuilder.Entity<Setting>().HasData(
            //    new Setting { Id = 3, Key = "Setting 3", Value = "3", UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") });
            //modelBuilder.Entity<Setting>().HasData(
            //    new Setting { Id = 4, Key = "Setting 4", Value = "4", UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") });

        }
    }
}
