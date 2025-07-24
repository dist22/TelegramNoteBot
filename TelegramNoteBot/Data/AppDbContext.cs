using Microsoft.EntityFrameworkCore;
using TelegramNoteBot.Models;

namespace TelegramNoteBot.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}