using Microsoft.EntityFrameworkCore;
using TelegramNoteBot.Models;

namespace TelegramNoteBot.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Tag> Tag => Set<Tag>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new NoteTagConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}