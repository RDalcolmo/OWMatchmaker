using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace OWMatchmaker.Models
{
    public partial class OWMatchmakerContext : DbContext
    {
        public OWMatchmakerContext()
        {
        }

        public OWMatchmakerContext(DbContextOptions<OWMatchmakerContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Lobbies> Lobbies { get; set; }
        public virtual DbSet<Matches> Matches { get; set; }
        public virtual DbSet<Players> Players { get; set; }
        public virtual DbSet<RegistrationMessages> RegistrationMessages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(Startup.Configuration["ConnectionString"]);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Lobbies>(entity =>
            {
                entity.HasKey(e => e.OwnerId)
                    .HasName("Lobbies_pkey");

                entity.HasIndex(e => e.LobbyId)
                    .HasName("Lobbies_LobbyID_key")
                    .IsUnique();

                entity.Property(e => e.OwnerId)
                    .HasColumnName("OwnerID")
                    .ValueGeneratedNever();

                entity.Property(e => e.LobbyId).HasColumnName("LobbyID");

                entity.HasOne(d => d.Owner)
                    .WithOne(p => p.Lobbies)
                    .HasForeignKey<Lobbies>(d => d.OwnerId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Lobbies_OwnerID_fkey");
            });

            modelBuilder.Entity<Matches>(entity =>
            {
                entity.HasKey(e => e.PlayerId)
                    .HasName("Matches_pkey");

                entity.Property(e => e.PlayerId)
                    .HasColumnName("PlayerID")
                    .ValueGeneratedNever();

                entity.Property(e => e.LobbyId).HasColumnName("LobbyID");

                entity.HasOne(d => d.Lobby)
                    .WithMany(p => p.Matches)
                    .HasPrincipalKey(p => p.LobbyId)
                    .HasForeignKey(d => d.LobbyId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Matches_LobbyID_fkey");

                entity.HasOne(d => d.Player)
                    .WithOne(p => p.Matches)
                    .HasForeignKey<Matches>(d => d.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Matches_PlayerID_fkey");
            });

            modelBuilder.Entity<Players>(entity =>
            {
                entity.HasKey(e => e.UserId)
                    .HasName("Players_pkey");

                entity.Property(e => e.UserId)
                    .HasColumnName("UserID")
                    .ValueGeneratedNever();

                entity.Property(e => e.Sr).HasColumnName("SR");
            });

            modelBuilder.Entity<RegistrationMessages>(entity =>
            {
                entity.HasKey(e => e.InitializedMessageId)
                    .HasName("RegistrationMessages_pkey");

                entity.Property(e => e.InitializedMessageId)
                    .HasColumnName("InitializedMessageID")
                    .ValueGeneratedNever();

                entity.Property(e => e.MessageId).HasColumnName("MessageID");

                entity.Property(e => e.OwnerId).HasColumnName("OwnerID");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
