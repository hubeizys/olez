using Microsoft.EntityFrameworkCore;
using ollez.Models;
using System.IO;

namespace ollez.Data
{
    public class ChatDbContext : DbContext
    {
        public DbSet<ChatSession> ChatSessions { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
        public DbSet<OllamaConfig> OllamaConfigs { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "chat.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 配置会话表
            modelBuilder.Entity<ChatSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // 配置消息表
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.IsUser).IsRequired();
                entity.Property(e => e.IsThinking).IsRequired();
                entity.Property(e => e.MessageType).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.SessionId).IsRequired();

                // 配置与会话的关系
                entity.HasOne(e => e.Session)
                      .WithMany(s => s.Messages)
                      .HasForeignKey(e => e.SessionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置Ollama配置表
            modelBuilder.Entity<OllamaConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InstallPath).IsRequired();
                entity.Property(e => e.ModelsPath).IsRequired();
                entity.Property(e => e.LastUpdated).IsRequired();
            });
        }
    }
}