using System;
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
        public DbSet<Process> Process { get; set; }
        public DbSet<State> State { get; set; }
        public DbSet<Result> Result { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.LogTo(Console.WriteLine);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Event>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable(nameof(Event));
                f.HasIndex(e => new { e.Id });
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<Cause>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable(nameof(Cause));
                f.HasIndex(e => new { e.Id });
                f.HasIndex(e => new { e.Id, e.EventId, e.ClassId });
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<Class>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable(nameof(Class));
                f.HasIndex(e => new { e.Id });
                f.HasIndex(e => new { e.Id, e.EventId });
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<Effect>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable(nameof(Effect));
                f.HasIndex(e => new { e.Id });
                f.HasIndex(e => new { e.Id, e.EventId, e.CauseId, e.ClassId, e.UserId });
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<Exclude>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable(nameof(Exclude));
                f.HasIndex(e => new { e.Id });
                f.HasIndex(e => new { e.Id, e.EventId, e.CauseId, e.UserId });
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<Meta>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable(nameof(Meta));
                f.HasIndex(e => new { e.Id });
                f.HasIndex(e => new { e.Id, e.EventId });
                f.HasIndex(e => new { e.Id, e.CauseId });
                f.HasIndex(e => new { e.Id, e.ClassId });
                f.HasIndex(e => new { e.Id, e.EffectId });
                f.HasIndex(e => new { e.Id, e.ExcludeId });
                f.HasIndex(e => new { e.Id, e.UserId });
                f.HasIndex(e => new { e.Id, e.ProcessId });
                f.HasIndex(e => new { e.Id, e.StateId });
                f.HasIndex(e => new { e.Id, e.ResultId });
                f.Property("Key").IsRequired();
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<Process>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable(nameof(Process));
                f.HasIndex(e => new { e.Id });
                f.HasIndex(e => new { e.Id, e.EventId });
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<State>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable(nameof(State));
                f.HasIndex(e => new { e.Id });
                f.HasIndex(e => new { e.Id, e.EventId, e.ProcessId });
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<Result>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable(nameof(Result));
                f.HasIndex(e => new { e.Id });
                f.HasIndex(e => new { e.Id, e.ProcessId, e.EventId, e.CauseId, e.ClassId, e.UserId });
                f.Property("Value").IsRequired();
                f.Property("UpdatedDate").IsRequired();
            });
            modelBuilder.Entity<User>(f =>
            {
                f.HasKey(e => e.Id);
                f.ToTable(nameof(User));
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
