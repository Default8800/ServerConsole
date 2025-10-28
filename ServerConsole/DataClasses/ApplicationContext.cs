using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerConsole.DataClasses
{
    internal class ApplicationContext : DbContext
    {
        public DbSet<Interfaces> Interfaces { get; set; } = null!;
        public DbSet<Devices> Devices { get; set; } = null!;

        public DbSet<Registers> Registers { get; set; } = null!;

        public DbSet<RegisterValues> RegisterValues { get; set; } = null!;
        public DbSet<Logs> Logs { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=test.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Interfaces>().ToTable("Interfaces");
            modelBuilder.Entity<Devices>().ToTable("Devices");
            modelBuilder.Entity<Registers>().ToTable("Registers");
            modelBuilder.Entity<RegisterValues>().ToTable("RegisterValues");
            modelBuilder.Entity<Logs>().ToTable("Logs");

            modelBuilder.Entity<Interfaces>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.EditingDate).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<Devices>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.FigureType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Color).IsRequired().HasMaxLength(50);
                entity.Property(e => e.InterfaceId).IsRequired();
                entity.Property(e => e.IsEnabled).IsRequired();
                entity.Property(e => e.Size).IsRequired();
                entity.Property(e => e.PosX).IsRequired();
                entity.Property(e => e.PosY).IsRequired();
                entity.Property(e => e.EditingDate).IsRequired();

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.InterfaceId);

                entity.HasOne(d => d.Interface)
                    .WithMany(i => i.Devices)
                    .HasForeignKey(d => d.InterfaceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Registers>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.DeviceId).IsRequired();
                entity.Property(e => e.EditingDate).IsRequired();

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.DeviceId);

                entity.HasOne(r => r.Device)
                    .WithMany(d => d.Registers)
                    .HasForeignKey(r => r.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RegisterValues>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RegisterId).IsRequired();
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();

                entity.HasIndex(e => e.RegisterId);
                entity.HasIndex(e => e.Timestamp);

                entity.HasOne(rv => rv.Register)
                    .WithMany(r => r.RegisterValues)
                    .HasForeignKey(rv => rv.RegisterId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Logs>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Timestamp).IsRequired();

                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.Type);
            });
        }
    }
}
