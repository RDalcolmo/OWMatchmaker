using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Birthday_Bot.Models
{
    public partial class OWMatchmakerContext : DbContext
    {
        private static DbContextOptions<OWMatchmakerContext> _options;

        public OWMatchmakerContext()
            : base(_options)
        {
        }

        public OWMatchmakerContext(DbContextOptions<OWMatchmakerContext> options)
            : base(options)
        {
            _options = options;
        }

        public virtual DbSet<Lobbies> Lobbies { get; set; }
        public virtual DbSet<Matches> Matches { get; set; }
        public virtual DbSet<Players> Players { get; set; }
        public virtual DbSet<ReactMessages> ReactMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Lobbies>(entity =>
            {
                entity.HasKey(e => e.OwnerId)
                    .HasName("Lobbies_pkey");

                entity.HasIndex(e => e.LobbyId)
                    .HasName("Lobbies_LobbyID_key")
                    .IsUnique();

                entity.HasIndex(e => e.MessageId)
                    .HasName("Lobbies_MessageID_key")
                    .IsUnique();

                entity.Property(e => e.OwnerId)
                    .HasColumnName("OwnerID")
                    .ValueGeneratedNever();

                entity.Property(e => e.LobbyId).HasColumnName("LobbyID");

                entity.Property(e => e.MessageId).HasColumnName("MessageID");
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
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Matches_LobbyID_fkey");

                entity.HasOne(d => d.Player)
                    .WithOne(p => p.Matches)
                    .HasForeignKey<Matches>(d => d.PlayerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
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

            modelBuilder.Entity<ReactMessages>(entity =>
            {
                entity.HasKey(e => e.MessageId)
                    .HasName("ReactMessages_pkey");

                entity.Property(e => e.MessageId)
                    .HasColumnName("MessageID")
                    .ValueGeneratedNever();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
